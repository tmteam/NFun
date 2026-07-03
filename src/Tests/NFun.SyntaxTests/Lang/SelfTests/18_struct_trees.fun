# 18_struct_trees.fun - tree algorithms with named struct type

type tree = {value: int, left: tree?, right: tree?}

fun treeSize(t):
    if t == none: return 0
    return 1 + treeSize(t?.left) + treeSize(t?.right)

fun treeDepth(t):
    if t == none: return 0
    return 1 + max(treeDepth(t?.left), treeDepth(t?.right))

fun countLeaves(t):
    if t == none: return 0
    if t?.left == none and t?.right == none: return 1
    return countLeaves(t?.left) + countLeaves(t?.right)

tree = {value = 1, left = {value = 2, left = {value = 4, left = none, right = none}, right = none}, right = {value = 3, left = none, right = none}}
size = treeSize(tree)
depth = treeDepth(tree)
leaves = countLeaves(tree)
sizeNone = treeSize(none)
sizeLeaf = treeSize({value = 42, left = none, right = none})
