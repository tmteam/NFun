# NFun Test Framework

The test framework lets a `.fun` file double as a unit-test suite. A function marked `@Test` is discovered and executed by the test runner; parameterized variants are produced from `@Test(args...)` lines.

## @Test annotations

`@Test` marks a function as a test. `@Test(args...)` provides parameterized arguments — one test invocation per `@Test` line. Multiple `@Test` lines may stack on the same function.

```py
@Test(1, 2, 3)
@Test(-1, 1, 0)
fun testAdd(a, b, expected):
    assertEqual(add(a, b), expected)

@Test
fun testBasic():
    assert(1 + 1 == 2)
    assertEqual(max(3, 5), 5)
```

A `@Test` without arguments calls the function with no parameters; the function must take none in that case.

## Test kit functions

These functions are available inside any test function. They throw on failure; the runner catches and reports.

| Function | Description |
|----------|-------------|
| `assert(cond)` | Fails if `cond` is false |
| `assertEqual(a, b)` | Fails if `a != b` |
| `assertNotEqual(a, b)` | Fails if `a == b` |
| `assertType(val, name)` | Fails if runtime type name of `val` doesn't match `name` |
