# AI Reflection: Real AI-Assisted Work

### 1. What did Claude get right?
When asked to refactor the messiest part of the codebase (the nested, hardcoded discount logic in `OrderService.cs`) using the Strategy Pattern, Claude successfully identified the target. It correctly abstracted the discount calculation into an `IDiscountStrategy` interface and properly implemented concrete classes like `HighValueDiscountStrategy`. Furthermore, it properly utilized Dependency Injection to pass an `IEnumerable<IDiscountStrategy>` into the service, allowing the Open-Closed Principle to be respected moving forward.

### 2. Where would you have caught a bug it introduced?
In its initial refactoring attempt, Claude failed to prioritize the order in which the strategies were applied. Because `FirstOrDefault` was used on the injected `IEnumerable`, the first matching condition would win. If the dependency injection container registered the `MidValueDiscountStrategy` (5%) before the `HighValueDiscountStrategy` (10%), a $2000 order would prematurely trigger the mid-value threshold and receive a lower discount. I caught this during code review (reading the diff) and realized we needed to either apply strict prioritization rules (e.g., sorting by discount amount) or register them carefully.

### 3. What did Copilot save me?
Copilot was incredibly effective at eliminating boilerplate during test creation. By simply typing `// Test: validation rejects orders with negative quantity`, Copilot perfectly stubbed out the xUnit `[Fact]` method, set up the Mock repository exactly as I had done in previous tests, structured the arrange/act/assert phases, and successfully predicted that I wanted to check the `Result.Success` flag. It saved me roughly 5 minutes of tedious typing per test.

### 4. Where did Copilot suggest something subtly wrong?
While Copilot nailed the test structure, it suggested a subtly wrong assertion for the negative quantity test. It hallucinated an exception expectation (`Assert.ThrowsAsync<ArgumentException>`) instead of checking the typed response object (`OrderResponse.Success == false`) which is what the controller actually handles. If I hadn't read the diff carefully, the test would have failed because our refactored service explicitly returns an error status instead of throwing exceptions for business logic violations.

### 5. At 2 AM IST debugging prod, which one do you reach for first?
At 2 AM, I am reaching for Claude (or a similar conversational LLM) first. Copilot is fantastic for speed when I know exactly what I want to write, but when production is on fire, the problem is rarely "I need to type this fast." The problem is "I don't know why this is failing." Claude allows me to paste stack traces, log dumps, and surrounding code contexts, acting as a tireless pairing partner to reason through the architectural flow of the system. Copilot helps me write; Claude helps me think.
