# LeetCode 0144 — Binary Tree Preorder Traversal
#
# Return the preorder traversal of a binary tree's node values.
# Self, left, right.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun preorder(t):
    if t == none: return []
    return concat(concat([t.value], preorder(t.left)), preorder(t.right))

@Test
fun testEmpty():
    assertEqual(preorder(none), [])

@Test
fun testFullSmall():
    root = tree {
        value = 2
        left = tree {value = 1}
        right = tree {value = 3}
    }
    assertEqual(preorder(root), [2, 1, 3])

@Test
fun testRightSkew():
    root = tree {
        value = 1
        right = tree {
            value = 2
            left = tree {value = 3}
        }
    }
    assertEqual(preorder(root), [1, 2, 3])
