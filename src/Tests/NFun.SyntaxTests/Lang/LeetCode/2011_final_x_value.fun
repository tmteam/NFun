# LeetCode 2011 — Final Value of Variable After Performing Operations
#
# `++X` and `X++` add 1; `--X` and `X--` subtract 1.

fun finalValueAfterOps(operations):
    x = 0
    for op in operations:
        # The middle character differentiates increment from decrement.
        if op[1] == /'+': x += 1
        else: x -= 1
    return x

@Test
fun testCanonical():
    assertEqual(finalValueAfterOps(['--X', 'X++', 'X++']), 1)

@Test
fun testAllUp():
    assertEqual(finalValueAfterOps(['++X', '++X', 'X++']), 3)

@Test
fun testMixed():
    assertEqual(finalValueAfterOps(['X++', '++X', '--X', 'X--']), 0)
