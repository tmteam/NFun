# LeetCode 1281 — Subtract the Product and Sum of Digits of an Integer

fun subtractProductAndSum(n):
    product = 1
    total = 0
    x = n
    while x > 0:
        d = x % 10
        product *= d
        total += d
        x = x // 10
    return product - total

@Test(234, 15)
@Test(4421, 21)
@Test(7, 0)
@Test(10, -1)
fun testSubProdSum(n, expected):
    assertEqual(subtractProductAndSum(n), expected)
