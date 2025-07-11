# Instructions for AI Agents

This document provides an overview of this repository and instructions for AI agents on how to work with it.

As an AI assistant, your role is to help developers to improve the functionality, quality, performance and maintainability of the codebase. This includes:

- **Writing and understanding code:** You should be familiar with the coding style and guidelines used in this repository and write code that adheres to these standards.
- **Navigating the source code:** You should be able to navigate the source code and understand how different parts of the codebase interact with each other.
- **Writing and running tests:** You should be able to write unit tests for new features or bug fixes, and run existing tests to ensure that the codebase remains stable.
- **Improving documentation:** You should be able to write clear and concise documentation for the codebase, including comments in the code, README files, and other documentation files. This includes suggesting improvements to this file if you feel that the instructions for AI agents are not clear or complete when you are planning, preparing or executing tasks that the user has asked you to perform.
- **Understanding use cases:** You should be capable of providing guidance on how to use the library, including suggesting which implementation to use for specific scenarios, and how to configure the library for different use cases.

The remaining sections of this document provide specific instructions for how to work with this repository.


# Repository Structure

Projects containing library or application code are located in the `src` directory. Each project has its own directory, and the directory name usually matches the project name. Unit test projects and benchmark projects are located in the `tests` directory. Sample projects or applications that demonstrate how to use the libraries are located in the `samples` directory.

Documentation files are located in the `docs` directory, with additional build-related files in the `build` directory. The root directory contains the main `README.md` file, which provides an overview of the project and how to get started.

The remote repository is located on GitHub. **Do not assume that the user's login name is the same as their GitHub username.** The remote repository URL can be found in the `Directory.Build.props`. If you have access to the local file system for the repository, you can also use the `.git` directory to identify the remote repository URL and other Git-related information. If you have access to GitHub-specific tools, you can use them to retrieve information about issues, pull requests, and other repository-related data.

**Do not work directly on the `main` branch.** Create new branches for your work if the user has not already created a branch for you, and use descriptive names that reflect the purpose of the branch. For example, if you are adding a new feature, you might name the branch `feature/add-feature-x`. If you are fixing a bug, you might name it `bugfix/fix-issue-123`. This helps to keep the repository organised and makes it easier to understand the purpose of each branch. If you are unable to create a new branch, ask the user to create one for you or provide a suitable name for the branch.

**When committing changes, ensure that the commit is signed if the user's Git configuration has a signing key configured unless the user has requested that you do not sign the commit.**

When you are ready to submit your changes, you may create a pull request against the default branch with the user's approval. Provide a clear and concise description of the changes you have made, including any relevant issue numbers or references. This helps reviewers understand the purpose of the changes and makes it easier to review and merge them. Ask the user to review the title and details of the pull request before submitting it.


# Code Style

The repository uses `.editorconfig` files to enforce code style and formatting rules. Make sure to follow these rules when writing or modifying code. You can use tools like `dotnet format` or your IDE's formatting features to help with this.

`.editorconfig` files can be located in multiple directories. Rules defined in a `.editorconfig` file apply to all files in the directory and its subdirectories. If there are multiple `.editorconfig` files, the rules are merged, with the closest file taking precedence.

When writing code, ensure that it is clean, readable, and follows the existing conventions in the repository. Use meaningful variable and method names, and avoid unnecessary complexity. Methods returning `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, or `IAsyncEnumerable<T>` should be suffixed with `Async` to indicate that they are asynchronous methods. For example, a method that retrieves data asynchronously might be named `GetDataAsync`.

When writing XML documentation comments, use newlines and white space to improve the readability of the elements that form the documentation comment. Wrap lines after 100 characters. For example:

```csharp
/// <summary>
/// Retrieves data for the specified ID.
/// </summary>
/// <param name="id">
///   The ID of the data to retrieve.
/// </param>
/// <param name="cancellationToken">
///   The cancellation token for the operation.
/// </param>
/// <returns>
///   A task that returns the data for the specified ID.
/// </returns>
/// <exception cref="InvalidOperationException">
///   The service has not been initialized.
/// </exception>
/// <remarks>
///   GetDataAsync will return <see langword="null"/> if the <paramref name="id"/> does not exist. 
///   Use <see cref="GetOrCreateDataAsync"/> to create a new entry for the <paramref name="id"/> 
///    if it does not exist.
/// </remarks>
public async Task<object?> GetDataAsync(int id, CancellationToken cancellationToken = default) {
    // ...
}
```

.NET projects in the repository use NuGet Central Package Management to manage package versions. This means that package versions are defined in the `Directory.Packages.props` file at the root of the repository. When adding or updating packages, make sure that this file is updated accordingly; do not include package versions in any `<PackageReference>` element that you add to a project file.

Common project properties and targets are defined in the `Directory.Build.props` and `Directory.Build.targets` files at the root of the repository. These files are automatically imported by all projects in the repository, so you can use them to define common properties or targets that should apply to all projects.


# Documentation Style and Naming Conventions

Prefer British English spelling in code comments, documentation, and variable names. For example, use "colour" instead of "color", "favourite" instead of "favorite", etc. This is to maintain consistency with the documentation style. Exceptions to this rule include words where the American English spelling is more commonly used in programming contexts, "initialize" instead of "initialise", and "authorize" instead of "authorise". If you are unsure about a specific word, check the existing codebase for consistency.

Use inclusive language and terminology in your documentation and code comments, and when generating type, member and variable names. Avoid using terms that may be considered offensive or exclusionary. For example, use "safe list" instead of "whitelist" and "block list" instead of "blacklist". Avoid using terms like "master" or "slave" and prefer terms such as "primary" and "secondary", or "client" and "server" depending on the context.

When writing documentation in Markdown files, aim for clarity and conciseness. Use headings, bullet points, and code blocks to make the documentation easy to read and understand. Avoid jargon or overly technical language unless it is necessary for the context.

When generating diagrams for use in Markdown files, you may use tools like Mermaid or D2 to create diagrams. Ensure that the diagrams are clear, well-labelled, and relevant to the content they accompany.


# Testing

When creating tests, the first iteration of the test should fail. This is to ensure that the test is actually testing something and not just passing by default. Once the test fails, you can then implement the code to make the test pass.

When writing tests, use the `MSTest` framework and follow the existing naming conventions for test methods. Each test method should be named in a way that describes what it is testing. Test classes should contain a `TestContext` property. Prefer to use `TestContext.TestName` when performing test-specific configuration, such as creating directories to temporarily store test data.

Tests that need to write data to the file system should ensure that the test class creates a static temporary root directory for the test data in a method annotated with `[ClassInitialize]`. The preferred way of creating the temporary directory path is `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())`. This directory should be cleaned up after the test is complete. Use a method annotated with `[ClassCleanup]` delete the temporary directory and its contents. Consult existing test classes for examples of how to implement this.

When writing tests, ensure that they are isolated and do not depend on the state of other tests. Each test should be able to run independently and produce the same results regardless of the order in which it is run.

Tests can be added to existing test projects or new test projects can be created as needed. Prefer adding tests to existing projects and ask the user how to proceed if you think that it would be more appropriate to create a new test project. Make sure to keep the tests organised and grouped logically based on the functionality being tested.


# Handling Complex Tasks

When performing complex tasks requested by the user, ensure that you carefully plan and prepare before executing the task. Summarise your plan to the user before you begin. **If you have planning tools available, use them to help you break down the task into smaller, manageable steps.** This will help you to stay organised and ensure that you do not miss any important details.
