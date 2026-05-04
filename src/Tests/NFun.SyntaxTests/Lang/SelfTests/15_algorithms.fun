# 15_algorithms.fun - classic algorithms

# -- 1. Fibonacci --

fun fib(n):
    if n <= 1: return n
    return fib(n - 1) + fib(n - 2)

@Test(0, 0)
@Test(1, 1)
@Test(5, 5)
@Test(10, 55)
fun testFib(n, expected):
    assertEqual(fib(n), expected)

# -- 2. GCD + LCM --

fun gcd(a, b):
    if b == 0: return a
    return gcd(b, a % b)

fun lcm(a, b):
    return a * b // gcd(a, b)

@Test(12, 8, 4)
@Test(100, 75, 25)
fun testGcd(a, b, expected):
    assertEqual(gcd(a, b), expected)

@Test(4, 6, 12)
@Test(3, 5, 15)
fun testLcm(a, b, expected):
    assertEqual(lcm(a, b), expected)

# -- 3. Fast exponentiation --

fun power(x, n):
    if n == 0: return 1
    if n % 2 == 0:
        half = power(x, n // 2)
        return half * half
    return x * power(x, n - 1)

@Test(2, 10, 1024)
@Test(3, 4, 81)
@Test(5, 0, 1)
@Test(7, 1, 7)
fun testPower(x, n, expected):
    assertEqual(power(x, n), expected)

# -- 4. Binary search --

fun binarySearch(arr, target):
    lo = 0
    hi = arr.count() - 1
    while lo <= hi:
        mid = (lo + hi) // 2
        if arr[mid] == target:
            return mid
        elif arr[mid] < target:
            lo = mid + 1
        else:
            hi = mid - 1
    return -1

@Test(23, 5)
@Test(2, 0)
@Test(91, 9)
@Test(100, -1)
fun testBinarySearch(target, expected):
    data = [2, 5, 8, 12, 16, 23, 38, 56, 72, 91]
    assertEqual(binarySearch(data, target), expected)

# -- 5. FizzBuzz --

fun fizzbuzz(n):
    if n % 15 == 0: return 'fizzbuzz'
    if n % 3 == 0: return 'fizz'
    if n % 5 == 0: return 'buzz'
    return n.toText()

@Test(1, '1')
@Test(3, 'fizz')
@Test(5, 'buzz')
@Test(15, 'fizzbuzz')
fun testFizzBuzz(n, expected):
    assertEqual(fizzbuzz(n), expected)

# -- 6. Factorial --

fun factorial(n):
    if n <= 1: return 1
    prev = factorial(n - 1)
    return n * prev

@Test(0, 1)
@Test(1, 1)
@Test(5, 120)
@Test(7, 5040)
fun testFactorial(n, expected):
    assertEqual(factorial(n), expected)

# -- 7. Sum via loop --

fun sum(arr):
    total = 0
    for item in arr:
        total += item
    return total

@Test
fun testSum():
    assertEqual(sum([1, 2, 3, 4, 5]), 15)
    assertEqual(sum([10, 20, 30]), 60)

# -- 8. Palindrome --

fun isPalindrome(s):
    return s == s.reverse()

@Test('racecar', true)
@Test('hello', false)
@Test('abba', true)
fun testPalindrome(s, expected):
    assertEqual(isPalindrome(s), expected)

# -- 9. Collatz conjecture --

fun collatzSteps(n):
    steps = 0
    while n != 1:
        if n % 2 == 0:
            n = n // 2
        else:
            n = 3 * n + 1
        steps += 1
    return steps

@Test(1, 0)
@Test(2, 1)
@Test(6, 8)
@Test(27, 111)
fun testCollatz(n, expected):
    assertEqual(collatzSteps(n), expected)

# -- 10. Count primes --

fun isPrime(n):
    if n < 2: return false
    i = 2
    while i * i <= n:
        if n % i == 0: return false
        i += 1
    return true

fun countPrimes(limit):
    count = 0
    i = 2
    while i <= limit:
        if isPrime(i): count += 1
        i += 1
    return count

@Test(10, 4)
@Test(20, 8)
@Test(100, 25)
fun testCountPrimes(limit, expected):
    assertEqual(countPrimes(limit), expected)
