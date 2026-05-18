# LeetCode 0235 — Lowest Common Ancestor of a BST
#
# In a BST the LCA of p and q is the deepest node whose value lies between
# them. Walk from root: descend left if both targets are smaller; right if
# both larger; otherwise the current node is the split point.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun lcaBST(root, p, q):
    node = root
    while node != none:
        if p < node.value and q < node.value:
            node = node.left
        elif p > node.value and q > node.value:
            node = node.right
        else:
            return node.value
    return -1

@Test
fun testCanonicalSplit():
    # BST:
    #         6
    #        / \
    #       2   8
    #      / \ / \
    #     0  4 7  9
    #       / \
    #      3   5
    root = tree {
        value = 6
        left = tree {
            value = 2
            left = tree {value = 0}
            right = tree {
                value = 4
                left = tree {value = 3}
                right = tree {value = 5}
            }
        }
        right = tree {
            value = 8
            left = tree {value = 7}
            right = tree {value = 9}
        }
    }
    assertEqual(lcaBST(root, 2, 8), 6)
    assertEqual(lcaBST(root, 0, 5), 2)
    assertEqual(lcaBST(root, 3, 5), 4)
    # When one target is an ancestor of the other, the ancestor itself is LCA.
    assertEqual(lcaBST(root, 2, 4), 2)
