# LeetCode 0645 — Set Mismatch
#
# Original is [1..n] with one number replaced by another (a duplicate).
# Algebra: sum trick recovers (dup - missing); sum of squares recovers
# (dup² - missing²). Solve the two linear equations.

fun findErrorNums(nums):
    n = nums.count()
    expectedSum = n * (n + 1) // 2
    expectedSqSum = n * (n + 1) * (2 * n + 1) // 6
    actualSum = 0
    actualSqSum = 0
    for x in nums:
        actualSum += x
        actualSqSum += x * x
    # dup - miss = actualSum - expectedSum = diff
    # dup + miss = (actualSqSum - expectedSqSum) / diff
    diff = actualSum - expectedSum
    sumPair = (actualSqSum - expectedSqSum) // diff
    dup = (diff + sumPair) // 2
    miss = sumPair - dup
    return [dup, miss]

@Test
fun testCanonical():
    assertEqual(findErrorNums([1, 2, 2, 4]), [2, 3])

@Test
fun testEndOff():
    assertEqual(findErrorNums([1, 1]), [1, 2])

@Test
fun testThree():
    assertEqual(findErrorNums([3, 2, 3, 4, 6, 5]), [3, 1])
