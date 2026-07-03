# LeetCode 0543 — Diameter of Binary Tree
#
# Diameter = number of edges on the longest path between any two nodes,
# not necessarily through the root. At each node the candidate is
# (left height + right height); we return the max over the tree.
#
# Standard trick: compute heights post-order and propagate the running max
# alongside. NFun doesn't have tuple returns, so use a small result struct.

type tree = {value: int, left: tree? = none, right: tree? = none}
type result = {height: int, diameter: int}

fun walk(t):
    if t == none: return result {height = 0, diameter = 0}
    L = walk(t!.left)
    R = walk(t!.right)
    return result {
        height = 1 + max(L.height, R.height)
        diameter = max(max(L.diameter, R.diameter), L.height + R.height)
    }

fun diameterOfBinaryTree(t):
    return walk(t).diameter

@Test
fun testEmpty():
    assertEqual(diameterOfBinaryTree(none), 0)

@Test
fun testSingleNode():
    assertEqual(diameterOfBinaryTree(tree {value = 1}), 0)

@Test
fun testCanonical():
    #     1
    #    / \
    #   2   3
    #  / \
    # 4   5            diameter = path 4-2-5 OR 4-2-1-3 = 3 edges
    root = tree {
        value = 1
        left = tree {
            value = 2
            left = tree {value = 4}
            right = tree {value = 5}
        }
        right = tree {value = 3}
    }
    assertEqual(diameterOfBinaryTree(root), 3)
