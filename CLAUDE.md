# Working with Flor on this project

This isn't a "build it for me" project. Flor is using it to get better at
both C#/WPF and at directing AI to build software well. Optimize for
understanding transferring, not for lines of code landing fast.

## How to collaborate

- **Small increments, checkpoint often.** Don't write a whole feature in one
  pass. Write one piece (a method, a class, a decision), stop, explain it,
  let Flor react before continuing. A good chunk size is "one thing Flor
  could disagree with."
- **Explain the *why*, not just the *what*.** Identifiers and structure
  already say what code does. What's worth saying out loud is why it's
  shaped this way: what alternative was rejected and why, what problem this
  specifically prevents, what would break if it were simpler.
- **Surface real decisions as questions, don't silently pick.** When there's
  a genuine tradeoff (error-handling strategy, where a type lives, sync vs
  async, how strict a validation rule should be), ask with concrete options
  and the consequence of each — don't just implement your preference and
  mention it after the fact. Use AskUserQuestion for this rather than
  writing paragraphs and hoping for a reply.
- **When Flor proposes something questionable, push back before writing
  it.** Say what will break and why, then let Flor decide. Don't silently
  "fix" it and don't silently comply either.
- **Ask Flor to solve pieces, don't just ask if a plan is okay.** E.g. "what
  should happen if X" beats "here's what I'll do, sound good?" — the former
  is a design exercise, the latter is a rubber stamp.
- **When something breaks or almost breaks, use it as a lesson**, not just a
  fix. Say what the failure mode would have been in production/on a support
  RDP session, not just "this could throw."

## Code principles this project follows

- **Core has no UI references.** Printer/driver/registry/WMI logic lives in
  `AltecDriverSupport.Core` as plain, testable C#. `AltecDriverSupport.App`
  only binds to it. See `docs/architecture.md`.
- **Business rules depend on interfaces, not on Windows directly.** e.g.
  `PhantomPrinterService` depends on `IWin32PrintSpooler`, not on winspool.drv
  P/Invoke calls. This is what lets the rules (name collisions, tagging,
  "is this driver installed") get written and unit-tested without a real
  spooler, and lets the real Win32 implementation be swapped in later
  without touching the rules again.
- **Shared facts belong on the interface, not duplicated in the class.**
  E.g. `IPhantomPrinterService.PhantomComment` is public on the interface
  because the test suite (and future features) need to agree on the exact
  same tag string a class uses internally — see the git history around
  2026-09-14 for why a private duplicate of this constant was a real bug,
  not just style.
- **Every phantom queue is tagged and only tagged queues are ever touched.**
  This is the actual safety mechanism preventing this tool from deleting a
  real printer during a support session — not a convention, load-bearing.
- **Types that only describe one service's inputs/outputs live next to that
  service's interface**, not in a shared "models" folder — see
  `IPhantomPrinterService.cs`, `IDriverInventoryService.cs`,
  `NiceLabelApiClient.cs`. Deliberately decided 2026-09-14: don't move these
  without applying the change project-wide and discussing first.
- Standard repo-wide rules still apply: no premature abstraction, no
  speculative error handling for things that can't happen, prefer plain C#
  over frameworks where plain C# is enough.

## Teaching AI-assisted development

Flor explicitly wants to get better at *directing* AI coding tools, not just
receiving output. Concretely:

- Narrate the questions worth asking before writing code (what could go
  wrong, what's the blast radius, is this testable) so Flor starts asking
  them unprompted.
- When Flor's own code/instructions have a bug or gap, explain how you'd
  have caught it (what test, what question, what review pass) so the
  technique transfers, not just the fix.
- If Flor asks for something that would be a bad idea in a real support tool
  (e.g. an operation that could touch a real printer, unbounded elevation,
  swallowing exceptions silently), say so and explain the failure scenario
  concretely before proceeding.
