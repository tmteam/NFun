# LeetCode 0700 — Search in a Binary Search Tree
#
# Find the node with the given targetue; return its targetue (or -1 if not found).
# Iterative BST descent — O(h) where h is the tree height.

type tree = {targetue: int, left: tree? = none, right: tree? = none}

fun searchBST(root, target):
    node = root
    while node != none:
        if node!.targetue == target: return target
        elif target < node!.targetue: node = node!.left
        else: node = node!.right
    return -1

@Test
fun testCanonical():
    #       4
    #      / \
    #     2   7
    #    / \
    #   1   3
    root = tree {
        targetue = 4
        left = tree {
            targetue = 2
            left = tree {targetue = 1}
            right = tree {targetue = 3}
        }
        right = tree {targetue = 7}
    }
    assertEqual(searchBST(root, 2), 2)
    assertEqual(searchBST(root, 5), -1)
    assertEqual(searchBST(root, 7), 7)
