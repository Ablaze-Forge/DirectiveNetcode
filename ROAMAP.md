# Directive Netcode Project Roadmap

This roadmap outlines the planned feature sets for the upcoming versions of the package.

**Note:** Development is currently feature-driven as it is the backbone for many of my projects and progression is tied to what features are needed and what logic errors are found during my use and reports.
This roadmap file is structured based on the expected version bump for each collection of features and may change as development evolves. The targets are in no particular order inside each Version section.

---------- 

### Version 0.1.0a (Alpha Preview)

This will be the **first use-ready release** that will be available on this repository and on a package manager. Expect bugs and instability.

- [ ] Complete Refactor of all classes and interfaces

- [ ] Concurrency support with Async methods and IEnumerable

- [ ] Basic authorization system with open implementation via interfaces

- [ ] Synchronization system
  
- [ ] Separation of the **ServerEngine**, **ClientEngine** and **MessageDispatcher** classes

- [ ] New **logging model** integrated as a first-class citizen across all classes.

----------

### Version 0.1.0b (Beta Stable)

The goal of this release is to stabilize the code based on the results from the use of the previous version  

- [ ] Final architectural validation of the core package structure.

- [ ] Bug fixing based on usage feedback from the alpha stage.

- [ ] Implementation of Unity testing ecosystem.

----------

### Version 0.2.0b (Feature Unstable Preview)

This release introduces new major networking features, making it temporarily unstable until subsequent stabilization.

- [ ] Concrete implementation of various pipelines for quicker message generation.

- [ ] Implementation of **Variable Synchronization** messages.

- [ ] Full **RPC (Remote Procedure Call)** system for method serialization, deserialization, and dynamic invocation.

----------

### Version 0.3.0 (Stable Minor Release)

This release focuses on finishing the features introduced during version **0.2.0b**.

- [ ] Data optimization pass to improve memory and network efficiency.

- [ ] Bug fixing release based on usage of the RPC and Pipeline systems.

----------

## Future Concepts (Beyond v0.3.0)

Features in this section currently are **not confirmed** and may change significantly, be postponed or even fully removed if not achievable with the system. Those features range from version 0.4.0 to 1.0.0 when the final release of the stable, battle tested package.

- [ ] Implementation of a dedicated system for asynchronous tasks via Unity's **Job System**.
See https://docs.unity3d.com/2022.3/Documentation/Manual/JobSystemOverview.html
