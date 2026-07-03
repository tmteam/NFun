# LeetCode 0094 — Binary Tree Inorder Traversal
#
# Return the inorder traversal of a binary tree's node values.
# Pure recursion on the named-struct tree type — left, self, right.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun inorder(t):
    if t == none: return []
    return concat(concat(inorder(t.left), [t.value]), inorder(t.right))

@Test
fun testSingleNode():
    assertEqual(inorder(tree {value = 5}), [5])

@Test
fun testEmpty():
    assertEqual(inorder(none), [])

@Test
fun testCanonical():
    # tree: 1 → right=2 → left=3   gives inorder [1, 3, 2]
    root = tree {
        value = 1
        right = tree {
            value = 2
            left = tree {value = 3}
        }
    }
    assertEqual(inorder(root), [1, 3, 2])

@Test
fun testFullSmall():
    #     2
    #    / \
    #   1   3
    root = tree {
        value = 2
        left = tree {value = 1}
        right = tree {value = 3}
    }
    assertEqual(inorder(root), [1, 2, 3])
