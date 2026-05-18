# LeetCode 0572 — Subtree of Another Tree
#
# Is `sub` a subtree of `root`? Search every node — if its subtree matches
# `sub` structurally and by value, return true.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun sameTree(p, q):
    if p == none and q == none: return true
    if p == none or q == none: return false
    if p.value != q.value: return false
    return sameTree(p.left, q.left) and sameTree(p.right, q.right)

fun isSubtree(root, sub):
    if root == none: return sub == none
    if sameTree(root, sub): return true
    return isSubtree(root.left, sub) or isSubtree(root.right, sub)

@Test
fun testCanonicalMatch():
    #     3                sub: 4
    #    / \                   / \
    #   4   5                 1   2
    #  / \
    # 1   2
    sub = tree {
        value = 4
        left = tree {value = 1}
        right = tree {value = 2}
    }
    root = tree {
        value = 3
        left = sub
        right = tree {value = 5}
    }
    assertEqual(isSubtree(root, sub), true)

@Test
fun testNoMatch():
    root = tree {
        value = 3
        left = tree {
            value = 4
            left = tree {value = 1}
            right = tree {
                value = 2
                left = tree {value = 0}
            }
        }
        right = tree {value = 5}
    }
    sub = tree {
        value = 4
        left = tree {value = 1}
        right = tree {value = 2}
    }
    assertEqual(isSubtree(root, sub), false)
