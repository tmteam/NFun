# LeetCode 0257 — Binary Tree Paths
#
# Return all root-to-leaf paths as strings of the form "v1->v2->v3".
# DFS collecting per-leaf strings into an array.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun walkPaths(t, prefix):
    if t == none: return []
    here = if prefix == '': '{t.value}' else: '{prefix}->{t.value}'
    if t.left == none and t.right == none:
        return [here]
    return concat(walkPaths(t.left, here), walkPaths(t.right, here))

fun binaryTreePaths(t):
    return walkPaths(t, '')

@Test
fun testEmpty():
    assertEqual(binaryTreePaths(none), [])

@Test
fun testLeaf():
    assertEqual(binaryTreePaths(tree {value = 1}), ['1'])

@Test
fun testCanonical():
    #     1
    #    / \
    #   2   3
    #    \
    #     5      paths: "1->2->5", "1->3"
    root = tree {
        value = 1
        left = tree {
            value = 2
            right = tree {value = 5}
        }
        right = tree {value = 3}
    }
    assertEqual(binaryTreePaths(root), ['1->2->5', '1->3'])
