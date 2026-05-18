# LeetCode 0404 — Sum of Left Leaves
#
# Sum the values of every leaf node that is the left child of its parent.
# Recurse with a flag tracking whether the current node was reached via a
# parent's left link.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun sumHelper(t, isLeft):
    if t == none: return 0
    if t.left == none and t.right == none:
        return if isLeft: t.value else: 0
    return sumHelper(t.left, true) + sumHelper(t.right, false)

fun sumOfLeftLeaves(t):
    return sumHelper(t, false)

@Test
fun testEmpty():
    assertEqual(sumOfLeftLeaves(none), 0)

@Test
fun testRootOnly():
    assertEqual(sumOfLeftLeaves(tree {value = 5}), 0)

@Test
fun testCanonical():
    # 3
    # ├ 9 (leaf, left)             → 9
    # └ 20
    #   ├ 15 (leaf, left)           → 15
    #   └ 7  (leaf, right)          → 0
    root = tree {
        value = 3
        left = tree {value = 9}
        right = tree {
            value = 20
            left = tree {value = 15}
            right = tree {value = 7}
        }
    }
    assertEqual(sumOfLeftLeaves(root), 24)
