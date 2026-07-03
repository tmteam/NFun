# LeetCode 0653 — Two Sum IV - Input is a BST
#
# Does the BST contain two distinct nodes whose sum equals k?
# Approach: inorder gives a sorted array, then two-pointer scan. O(n) after
# the traversal.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun inorder(t):
    if t == none: return []
    return concat(concat(inorder(t.left), [t.value]), inorder(t.right))

fun findTarget(root, k):
    arr = inorder(root)
    lo = 0
    hi = arr.count() - 1
    while lo < hi:
        s = arr[lo] + arr[hi]
        if s == k: return true
        elif s < k: lo += 1
        else: hi -= 1
    return false

@Test
fun testCanonicalFound():
    #         5
    #        / \
    #       3   6
    #      / \   \
    #     2   4   7
    root = tree {
        value = 5
        left = tree {
            value = 3
            left = tree {value = 2}
            right = tree {value = 4}
        }
        right = tree {
            value = 6
            right = tree {value = 7}
        }
    }
    assertEqual(findTarget(root, 9), true)
    assertEqual(findTarget(root, 28), false)

@Test
fun testSingleNode():
    assertEqual(findTarget(tree {value = 5}, 10), false)
