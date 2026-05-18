# LeetCode 0938 — Range Sum of BST
#
# Sum values in [low, high] inclusive. BST property prunes whole subtrees:
# if current < low, skip the left subtree; if current > high, skip the right.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun rangeSumBST(root, low, high):
    if root == none: return 0
    if root.value < low: return rangeSumBST(root.right, low, high)
    if root.value > high: return rangeSumBST(root.left, low, high)
    return root.value + rangeSumBST(root.left, low, high) + rangeSumBST(root.right, low, high)

@Test
fun testCanonical():
    #         10
    #        /  \
    #       5    15
    #      / \     \
    #     3   7     18
    root = tree {
        value = 10
        left = tree {
            value = 5
            left = tree {value = 3}
            right = tree {value = 7}
        }
        right = tree {
            value = 15
            right = tree {value = 18}
        }
    }
    assertEqual(rangeSumBST(root, 7, 15), 32)
    assertEqual(rangeSumBST(root, 6, 10), 17)

@Test
fun testEmpty():
    assertEqual(rangeSumBST(none, 1, 100), 0)
