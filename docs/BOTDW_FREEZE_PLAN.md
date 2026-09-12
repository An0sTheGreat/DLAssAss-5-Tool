# BOTDW Freeze Plan

Status: Deferred at the user's request. Not addressed by manager v1.0.2.

## Report and evidence

After alt-tabbing or leaving Blood of Dawnwalker unfocused, returning and toggling Neural Rendering or adjusting its settings can freeze the game image until restart.

The supplied log identifies addon `1.0.3-pass-controls.2`. It records successful NR evaluations during setting transitions, but this does not establish GPU completion or successful presentation. The cause is not confirmed. Preview 3 changes layout and version metadata, not this behavior.

## Implementation plan for later

1. Trace focus loss/resume, NR toggles and settings changes through resource retirement, queue synchronization and presentation.
2. Fix the shared transition path identified by the investigation, preserving the working multipass and flicker fixes. Do not reset or release in-flight GPU resources based on callback activity alone.
3. Add regression coverage for interrupted rendering followed by toggles, resolution and pass-count changes.
4. Build and validate a separate preview for manual installation. Leave installed games untouched.

Resume only after user confirmation. Do not describe this freeze as fixed without evidence from the relevant game sequence.
