# LeetCode 0762 — Prime Number of Set Bits in Binary Representation
#
# Count integers in [left, right] whose popcount is prime. For 32-bit input,
# popcount ≤ 32 — primes in that range fit in a precomputed table.

fun popcount(n):
    c = 0
    x = n
    while x != 0:
        x = x & (x - 1)
        c += 1
    return c

fun isPrimeSmall(p):
    if p < 2: return false
    return p in [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31]

fun countPrimeSetBits(left, right):
    count = 0
    i = left
    while i <= right:
        if isPrimeSmall(popcount(i)): count += 1
        i += 1
    return count

@Test(6, 10, 4)
@Test(10, 15, 5)
fun testCountPrimeSetBits(left, right, expected):
    assertEqual(countPrimeSetBits(left, right), expected)
