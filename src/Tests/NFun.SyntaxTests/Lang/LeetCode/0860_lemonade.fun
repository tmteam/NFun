# LeetCode 0860 — Lemonade Change
#
# Customers pay $5/$10/$20 in order; we start with no change. Track $5 and
# $10 counts (any change >$10 can always be made up by $5s; $20 bills aren't
# usable as change).

fun lemonadeChange(bills):
    five = 0
    ten = 0
    for b in bills:
        if b == 5:
            five += 1
        elif b == 10:
            if five == 0: return false
            five -= 1
            ten += 1
        else:
            # b == 20 — prefer a $10 + $5, fall back to three $5s
            if ten > 0 and five > 0:
                ten -= 1
                five -= 1
            elif five >= 3:
                five -= 3
            else:
                return false
    return true

@Test
fun testCanonical():
    assertEqual(lemonadeChange([5, 5, 5, 10, 20]), true)

@Test
fun testNoChange():
    assertEqual(lemonadeChange([10, 10]), false)

@Test
fun testCannotMake20():
    assertEqual(lemonadeChange([5, 5, 10, 10, 20]), false)
