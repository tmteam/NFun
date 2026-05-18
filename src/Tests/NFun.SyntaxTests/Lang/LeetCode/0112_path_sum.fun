# LeetCode 0112 — Path Sum
#
# Does the tree have a root-to-leaf path whose values sum to `targetSum`?
# Subtract from target as we descend; check when we land on a leaf.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun hasPathSum(t, targetSum):
    if t == none: return false
    rest = targetSum - t.value
    if t.left == none and t.right == none:
        return rest == 0
    return hasPathSum(t.left, rest) or hasPathSum(t.right, rest)

@Test
fun testEmpty():
    assertEqual(hasPathSum(none, 0), false)

@Test
fun testLeaf():
    assertEqual(hasPathSum(tree {value = 5}, 5), true)
    assertEqual(hasPathSum(tree {value = 5}, 4), false)

@Test
fun testCanonical():
    # path 5 → 4 → 11 → 2 sums to 22
    root = tree {
        value = 5
        left = tree {
            value = 4
            left = tree {
                value = 11
                left = tree {value = 7}
                right = tree {value = 2}
            }
        }
    }
    assertEqual(hasPathSum(root, 22), true)
    assertEqual(hasPathSum(root, 23), false)
