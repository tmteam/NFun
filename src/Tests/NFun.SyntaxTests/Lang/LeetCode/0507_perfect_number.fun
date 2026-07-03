# LeetCode 0507 — Perfect Number
#
# Sum of proper divisors equals the number. Walk up to √n collecting divisor
# pairs.

fun checkPerfectNumber(num):
    if num <= 1: return false
    total = 1
    i = 2
    while i * i <= num:
        if num % i == 0:
            total += i
            partner = num // i
            if partner != i: total += partner
        i += 1
    return total == num

@Test(28, true)
@Test(6, true)
@Test(496, true)
@Test(8128, true)
@Test(7, false)
@Test(1, false)
@Test(0, false)
fun testPerfectNumber(num, expected):
    assertEqual(checkPerfectNumber(num), expected)
