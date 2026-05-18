# LeetCode 2331 — Evaluate Boolean Binary Tree
#
# Leaf values are 0/1 (false/true). Internal node 2 = OR, 3 = AND.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun evaluateTree(root):
    if root == none: return false
    if root.left == none and root.right == none:
        return root.value == 1
    L = evaluateTree(root.left)
    R = evaluateTree(root.right)
    if root.value == 2: return L or R
    return L and R

@Test
fun testCanonical():
    # OR(true, AND(false, true)) = true
    root = tree {
        value = 2
        left = tree {value = 1}
        right = tree {
            value = 3
            left = tree {value = 0}
            right = tree {value = 1}
        }
    }
    assertEqual(evaluateTree(root), true)

@Test
fun testSingleLeaf():
    assertEqual(evaluateTree(tree {value = 0}), false)
    assertEqual(evaluateTree(tree {value = 1}), true)
