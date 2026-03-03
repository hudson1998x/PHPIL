## Unit Testing PHPIL

All engine tests are located in the `Tests/Engine` folder. Keep tests **well-organized** and **modular** to make maintenance and debugging easier.

**Guidelines:**

* Each parser, pattern, or syntax node should have its own dedicated test file.
* Name test methods clearly to reflect what they validate.
* Cover edge cases, nested structures, and expected failures.
* All pull requests must run the full test suite to prevent regressions.

Maintaining comprehensive and organized tests ensures the PHPIL engine stays reliable and easier to extend.