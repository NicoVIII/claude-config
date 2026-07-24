#!/usr/bin/env -S cargo +nightly -Zscript
---
[package]
edition = "2024"

[dependencies]
serde = { version = "1", features = ["derive"] }
serde_json = "1"
---
//! Deterministic gather pass for the `prioritize` skill.
//!
//! Everything here is a fixed gh pipeline with no judgement in it, so it runs as
//! code rather than as prose the model has to reproduce correctly each time. It
//! prints a digest of roughly 2 KB in place of the ~30 KB of raw JSON the queries
//! return, keeping the model's context for ranking.
//!
//! Rows that only feed a count (self-authored issues, green bot PRs) are counted
//! here and never printed individually. Rows needing a judgement call land in the
//! ATTENTION block.
//!
//! Deserializing into named structs rather than shaping with jq is the point of
//! the rewrite: a field gh renames or drops becomes a loud parse error instead of
//! a silent null that reads downstream as "nothing open here".

use serde::Deserialize;
use serde::de::DeserializeOwned;
use std::collections::BTreeMap;
use std::process::Command;
use std::sync::Mutex;
use std::sync::atomic::{AtomicUsize, Ordering};
use std::thread;

/// gh's list commands default to 30 rows and truncate in silence.
const LIMIT: usize = 200;

/// This is ~90 network round trips and they dominate wall time.
const PARALLEL: usize = 8;

const TITLE_WIDTH: usize = 64;

/// Aborts the whole process, worker threads included. A repo missing from this
/// digest reads exactly like a repo with nothing wrong, so a partial run must
/// never reach stdout.
fn die(message: impl AsRef<str>) -> ! {
    eprintln!("gather: {}", message.as_ref());
    std::process::exit(1);
}

/// Runs a gh invocation. A failure whose stderr contains `tolerated` yields None
/// — a per-repo feature being switched off is a fact, not an error. Every other
/// failure aborts.
fn gh(args: &[&str], tolerated: Option<&str>) -> Option<String> {
    let output = Command::new("gh")
        .args(args)
        .output()
        .unwrap_or_else(|e| die(format!("could not run gh: {e}")));

    if output.status.success() {
        return Some(String::from_utf8_lossy(&output.stdout).into_owned());
    }

    let stderr = String::from_utf8_lossy(&output.stderr);
    match tolerated {
        Some(pattern) if stderr.contains(pattern) => None,
        _ => die(format!("gh {} failed: {}", args.join(" "), stderr.trim())),
    }
}

fn gh_json<T: DeserializeOwned>(args: &[&str], tolerated: Option<&str>) -> Option<T> {
    let raw = gh(args, tolerated)?;
    match serde_json::from_str(&raw) {
        Ok(parsed) => Some(parsed),
        Err(e) => die(format!(
            "gh {} returned JSON this script cannot read ({e}); the fields it asks for have probably changed",
            args.join(" ")
        )),
    }
}

/// A result arriving at the ceiling means the query truncated and the digest is
/// incomplete — which this script exists to prevent.
fn guard_truncation<T>(rows: Vec<T>, what: &str) -> Vec<T> {
    if rows.len() >= LIMIT {
        die(format!(
            "{what} returned {} rows at the {LIMIT} limit; raise LIMIT",
            rows.len()
        ));
    }
    rows
}

fn day(timestamp: &str) -> String {
    timestamp.chars().take(10).collect()
}

fn clip(text: &str, width: usize) -> String {
    if text.chars().count() <= width {
        return text.to_string();
    }
    text.chars().take(width - 1).chain(['…']).collect()
}

#[derive(Deserialize)]
struct Owner {
    login: String,
}

/// `is_bot` defaults because `gh search prs` omits it; bot-ness is unused on
/// that path, where every hit is weight 1 regardless of who opened it.
#[derive(Deserialize)]
struct Author {
    login: String,
    #[serde(default)]
    is_bot: bool,
}

#[derive(Deserialize)]
struct BranchRef {
    name: String,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct Repo {
    name: String,
    owner: Owner,
    pushed_at: String,
    default_branch_ref: Option<BranchRef>,
}

#[derive(Deserialize)]
struct Check {
    #[serde(default)]
    conclusion: Option<String>,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct Pull {
    number: u64,
    title: String,
    author: Author,
    updated_at: String,
    #[serde(default)]
    status_check_rollup: Option<Vec<Check>>,
}

#[derive(Deserialize)]
struct Issue {
    number: u64,
    author: Author,
}

#[derive(Deserialize)]
struct Comment {
    author: Author,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct IssueDetail {
    title: String,
    author: Author,
    created_at: String,
    comments: Vec<Comment>,
}

#[derive(Deserialize)]
struct Run {
    conclusion: Option<String>,
}

#[derive(Deserialize)]
struct Advisory {
    severity: String,
}

#[derive(Deserialize)]
struct Alert {
    security_advisory: Advisory,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct RepoName {
    name_with_owner: String,
}

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct SearchPull {
    number: u64,
    title: String,
    repository: RepoName,
    author: Author,
    updated_at: String,
}

#[derive(Deserialize)]
struct User {
    login: String,
}

/// One line of the ATTENTION block: something a human has to weigh, as opposed
/// to something that only needs counting.
struct Attention {
    kind: &'static str,
    target: String,
    who: String,
    state: String,
    date: String,
    title: String,
}

struct Row {
    repo: String,
    pulls: usize,
    issues: usize,
    ci: String,
    alerts: String,
    pushed: String,
}

struct Report {
    row: Row,
    attention: Vec<Attention>,
}

/// Counts every open PR but surfaces only those needing a decision: failing CI,
/// or a human author. Green bot PRs are cluster-line material.
fn pull_signal(repo: &str, attention: &mut Vec<Attention>) -> usize {
    let limit = LIMIT.to_string();
    let pulls: Vec<Pull> = gh_json(
        &[
            "pr",
            "list",
            "-R",
            repo,
            "--limit",
            &limit,
            "--json",
            "number,title,author,statusCheckRollup,updatedAt",
        ],
        None,
    )
    .expect("pr list declares no tolerated failure");
    let pulls = guard_truncation(pulls, &format!("{repo} PRs"));

    for pull in &pulls {
        let failing = pull
            .status_check_rollup
            .as_deref()
            .unwrap_or_default()
            .iter()
            .any(|check| check.conclusion.as_deref() == Some("FAILURE"));

        if !failing && pull.author.is_bot {
            continue;
        }
        attention.push(Attention {
            kind: "pr",
            target: format!("{repo}#{}", pull.number),
            who: pull.author.login.clone(),
            state: if failing { "FAILING-CI" } else { "human" }.to_string(),
            date: day(&pull.updated_at),
            title: clip(&pull.title, TITLE_WIDTH),
        });
    }
    pulls.len()
}

/// Only an issue someone else opened can put me on the hook, and whether it was
/// answered needs the thread — updatedAt does not show it, since a label edit
/// bumps that too. That costs one call per external issue, which is a handful.
fn issue_signal(repo: &str, me: &str, attention: &mut Vec<Attention>) -> usize {
    let limit = LIMIT.to_string();
    let issues: Vec<Issue> = gh_json(
        &[
            "issue", "list", "-R", repo, "--limit", &limit, "--json", "number,author",
        ],
        Some("has disabled issues"),
    )
    .unwrap_or_default();
    let issues = guard_truncation(issues, &format!("{repo} issues"));

    for issue in issues.iter().filter(|issue| issue.author.login != me) {
        let number = issue.number.to_string();
        let detail: IssueDetail = gh_json(
            &[
                "issue",
                "view",
                &number,
                "-R",
                repo,
                "--json",
                "title,author,createdAt,comments",
            ],
            None,
        )
        .expect("issue view declares no tolerated failure");

        let answered = detail.comments.iter().any(|c| c.author.login == me);
        attention.push(Attention {
            kind: "issue",
            target: format!("{repo}#{number}"),
            who: detail.author.login,
            state: format!(
                "{} {} replies",
                if answered { "answered" } else { "UNANSWERED" },
                detail.comments.len()
            ),
            date: day(&detail.created_at),
            title: clip(&detail.title, TITLE_WIDTH),
        });
    }
    issues.len()
}

/// The branch is resolved per repo because `gh run list --limit 1` without one
/// returns the newest run on any branch, which reads healthy while main is red.
fn ci_signal(repo: &str, branch: Option<&str>) -> String {
    let Some(branch) = branch else {
        return "no-branch".to_string();
    };
    let runs: Vec<Run> = gh_json(
        &[
            "run",
            "list",
            "-R",
            repo,
            "--branch",
            branch,
            "--limit",
            "1",
            "--json",
            "conclusion",
        ],
        None,
    )
    .expect("run list declares no tolerated failure");

    match runs.first() {
        None => "no-runs".to_string(),
        Some(run) => run
            .conclusion
            .clone()
            .unwrap_or_else(|| "in-progress".to_string()),
    }
}

fn severity_rank(severity: &str) -> u8 {
    match severity {
        "critical" => 0,
        "high" => 1,
        "medium" => 2,
        "low" => 3,
        _ => 4,
    }
}

/// Severities rather than a bare count: one critical outranks a pile of lows.
/// A 403 carrying "Dependabot alerts are disabled" is that repo's setting. gh
/// appends a boilerplate hint about admin:repo_hook scope to any 403 — noise,
/// disproved by the other repos in the same run answering with data.
fn alert_signal(repo: &str) -> String {
    let path = format!("repos/{repo}/dependabot/alerts?state=open");
    let Some(alerts): Option<Vec<Alert>> =
        gh_json(&["api", &path], Some("Dependabot alerts are disabled"))
    else {
        return "disabled".to_string();
    };
    if alerts.is_empty() {
        return "-".to_string();
    }

    let mut counts: BTreeMap<(u8, String), usize> = BTreeMap::new();
    for alert in &alerts {
        let severity = alert.security_advisory.severity.clone();
        *counts
            .entry((severity_rank(&severity), severity))
            .or_default() += 1;
    }
    counts
        .iter()
        .map(|((_, severity), count)| format!("{severity}:{count}"))
        .collect::<Vec<_>>()
        .join(" ")
}

fn gather_repo(repo: &Repo, me: &str) -> Report {
    let full_name = format!("{}/{}", repo.owner.login, repo.name);
    let mut attention = Vec::new();
    let pulls = pull_signal(&full_name, &mut attention);
    let issues = issue_signal(&full_name, me, &mut attention);
    let ci = ci_signal(
        &full_name,
        repo.default_branch_ref.as_ref().map(|b| b.name.as_str()),
    );
    let alerts = alert_signal(&full_name);

    Report {
        row: Row {
            repo: full_name,
            pulls,
            issues,
            ci,
            alerts,
            pushed: day(&repo.pushed_at),
        },
        attention,
    }
}

fn gather_all(repos: &[Repo], me: &str) -> Vec<Report> {
    let cursor = AtomicUsize::new(0);
    let collected = Mutex::new(Vec::new());

    thread::scope(|scope| {
        for _ in 0..PARALLEL.min(repos.len()) {
            scope.spawn(|| {
                loop {
                    let index = cursor.fetch_add(1, Ordering::Relaxed);
                    let Some(repo) = repos.get(index) else { break };
                    let report = gather_repo(repo, me);
                    collected.lock().unwrap().push(report);
                }
            });
        }
    });

    collected.into_inner().unwrap()
}

/// `gh repo list` only enumerates repos I own, so a review request in someone
/// else's or an org's repo never reaches the loop above. Weight 1 by definition.
fn review_requests() -> Vec<Attention> {
    let found: Vec<SearchPull> = gh_json(
        &[
            "search",
            "prs",
            "--review-requested=@me",
            "--state=open",
            "--limit",
            "100",
            "--json",
            "number,title,repository,author,updatedAt",
        ],
        None,
    )
    .expect("pr search declares no tolerated failure");

    found
        .into_iter()
        .map(|pull| Attention {
            kind: "review",
            target: format!("{}#{}", pull.repository.name_with_owner, pull.number),
            who: pull.author.login,
            state: "REVIEW-REQUESTED".to_string(),
            date: day(&pull.updated_at),
            title: clip(&pull.title, TITLE_WIDTH),
        })
        .collect()
}

fn kind_rank(kind: &str) -> u8 {
    match kind {
        "review" => 0,
        "issue" => 1,
        _ => 2,
    }
}

fn render(mut reports: Vec<Report>, mut attention: Vec<Attention>) {
    reports.sort_by_key(|report| report.row.repo.to_lowercase());
    attention.extend(reports.iter_mut().flat_map(|r| r.attention.drain(..)));

    // Oldest first within a kind: age is what makes these urgent.
    attention.sort_by(|a, b| {
        kind_rank(a.kind)
            .cmp(&kind_rank(b.kind))
            .then_with(|| a.date.cmp(&b.date))
    });

    let repo_width = reports
        .iter()
        .map(|r| r.row.repo.len())
        .chain([4])
        .max()
        .unwrap();
    println!(
        "{:repo_width$}  {:>3}  {:>5}  {:<11}  {:<24}  {}",
        "REPO", "PR", "ISSUE", "CI", "ALERTS", "PUSHED"
    );
    for Report { row, .. } in &reports {
        println!(
            "{:repo_width$}  {:>3}  {:>5}  {:<11}  {:<24}  {}",
            row.repo, row.pulls, row.issues, row.ci, row.alerts, row.pushed
        );
    }

    println!("\nATTENTION (everything else is cluster-line material)");
    if attention.is_empty() {
        println!("none");
        return;
    }
    let target_width = attention.iter().map(|a| a.target.len()).max().unwrap();
    let who_width = attention.iter().map(|a| a.who.len()).max().unwrap();
    let state_width = attention.iter().map(|a| a.state.len()).max().unwrap();
    for item in &attention {
        println!(
            "{:<6} {:target_width$}  {:who_width$}  {:state_width$}  {}  {}",
            item.kind, item.target, item.who, item.state, item.date, item.title
        );
    }
}

fn main() {
    let user: User = gh_json(&["api", "user"], None).expect("user lookup declares no tolerated failure");
    let limit = LIMIT.to_string();
    let repos: Vec<Repo> = gh_json(
        &[
            "repo",
            "list",
            "--no-archived",
            "--source",
            "--limit",
            &limit,
            "--json",
            "name,owner,pushedAt,defaultBranchRef",
        ],
        None,
    )
    .expect("repo list declares no tolerated failure");
    let repos = guard_truncation(repos, "repo list");
    if repos.is_empty() {
        die("gh repo list returned nothing");
    }

    render(gather_all(&repos, &user.login), review_requests());
}
