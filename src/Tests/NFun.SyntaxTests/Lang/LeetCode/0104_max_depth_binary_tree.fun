# LeetCode 0104 — Maximum Depth of Binary Tree
#
# Length of the longest path from root to leaf. Trivial post-order recursion.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun maxDepth(t):
    if t == none: return 0
    return 1 + max(maxDepth(t.left), maxDepth(t.right))

@Test
fun testEmpty():
    assertEqual(maxDepth(none), 0)

@Test
fun testLeaf():
    assertEqual(maxDepth(tree {value = 1}), 1)

@Test
fun testTriangle():
    root = tree {
        value = 1
        left = tree {value = 2}
        right = tree {value = 3}
    }
    assertEqual(maxDepth(root), 2)

@Test
fun testLeftSkew():
    root = tree {
        value = 1
        left = tree {
            value = 2
            left = tree {value = 3}
        }
    }
    assertEqual(maxDepth(root), 3)
