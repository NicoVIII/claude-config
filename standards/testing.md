# Testing

Applies when writing, placing, or reviewing tests, deciding whether a change needs one, or verifying by hand that a change works — in any language.

## What a test asserts

- **Behaviour through an interface: the return value and the resulting state.** Never the order of calls, whether a collaborator was called, or private state.
- **An assertion that only shows the code ran, or only checks a shape, counts as a missing test.**
- **Never test what the types guarantee.** Types own shape, illegal states, and exhaustiveness. Tests own behaviour and the boundaries types cannot see.
- **Replace a dependency with a working in-memory implementation, and assert on its resulting state.** No mocks, no spies.
- **A test that breaks under a behaviour-preserving refactor is deleted and rewritten, not patched.**

## Which tests exist

- **Some tests are owed and the rest are earned.** Owed tests together cover the whole behaviour. A unit test is earned by logic complex enough to need fast feedback, and a thin wrapper never earns one.
- **Owed for a slice with a core:** its behaviour driven through its entry point below the transport, with the dependencies it owns faked; each mapping between the transport and the core, as the pure function it is; each adapter against the real thing or a throwaway instance of it.
- **Owed for a program that is mostly shell:** the built program driven against a throwaway root, in place of tests for its layers.
- **Glue that only wires tested pieces together gets no test of its own.**
- **Property-based by default for pure domain logic.** Example-based for known cases and regressions.
- **A fixed bug is owed a test that reproduces it, at the lowest level that can express it,** written before the fix or with it. Where no existing level can express it, name that as an accepted gap and never skip it silently.
- **Coverage is judged, never measured.** No percentage serves as a target or as a verdict.

## Where tests live

- **Owed tests are grouped by what they cover, unit tests by where their subject lives** ([decision](decisions/0001-two-axis-test-layout.md)).
- **A unit test is findable from its subject by path:** beside it where the toolchain allows, at the mirrored path otherwise.
- **An empty directory for an owed test stays, as a marker of the debt.** A directory for unit tests is never created ahead of its first test.
- **Selective runs go on tags, never into directory names.**

## How a test reads

- Mark arrange, act, and assert.
- Setup lives in named helpers, so the body shows only what the test is about.

## Verifying by hand

- **Verified means the behaviour was observed through a real interface and the persisted state was inspected.** A status code or a log line alone is not evidence.
- **Anything that mutates state runs against a scratch copy,** never the instance that holds real data.
- **Every verdict states what was not covered.**
- **Scripts written to drive a verification are thrown away.** They are not committed as tests and not wired into CI.

## Not settled — raise the trade-off, leave the call to me

- **Proving a fake matches the real adapter.** Owed tests trust the fake, adapter tests exercise the real thing, and nothing proves the two agree. A suite run against both closes that gap at real cost.
- **Tests through the real transport, end to end.** Not added unless I ask; thin when they are.
- **Pinning the calls made to an external program.** A stub that answers only the calls a scenario lists makes a changed flag fail, which is the point of it, and is also an assertion on a call. Whether the boundary to a program I don't own is where that rule stops is open.
