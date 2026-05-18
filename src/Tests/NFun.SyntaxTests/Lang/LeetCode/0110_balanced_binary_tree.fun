# LeetCode 0110 — Balanced Binary Tree
#
# Heights of the two subtrees differ by at most 1 at every node. Single
# post-order pass returning either subtree height or `-1` for "already
# unbalanced" — avoids recomputing depth.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun checkHeight(t):
    if t == none: return 0
    lh = checkHeight(t.left)
    if lh == -1: return -1
    rh = checkHeight(t.right)
    if rh == -1: return -1
    if abs(lh - rh) > 1: return -1
    return 1 + max(lh, rh)

fun isBalanced(t):
    return checkHeight(t) != -1

@Test
fun testEmpty():
    assertEqual(isBalanced(none), true)

@Test
fun testBalanced():
    root = tree {
        value = 2
        left = tree {value = 1}
        right = tree {value = 3}
    }
    assertEqual(isBalanced(root), true)

@Test
fun testLeftSkewUnbalanced():
    root = tree {
        value = 1
        left = tree {
            value = 2
            left = tree {value = 3}
        }
    }
    assertEqual(isBalanced(root), false)
