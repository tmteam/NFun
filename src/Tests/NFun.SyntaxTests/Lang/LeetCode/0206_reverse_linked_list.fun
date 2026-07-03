# LeetCode 0206 — Reverse Linked List
#
# Build a reversed copy. Iterative accumulator: prepend each value to a new
# running list. `rev` is declared once with explicit type and then rebound
# inside the loop; without the explicit `:node?` annotation TIC can't infer
# the recursive optional type from `none` alone.

type node = {value: int, next: node? = none}

fun reverseList(head: node?) -> node?:
    rev:node? = none
    cur = head
    while cur != none:
        rev = node {value = cur.value, next = rev}
        cur = cur.next
    return rev

fun toArray(n: node?):
    out = []
    cur = n
    while cur != none:
        out = concat(out, [cur.value])
        cur = cur.next
    return out

@Test
fun testCanonical():
    head = node {value = 1, next =
            node {value = 2, next =
              node {value = 3, next =
                node {value = 4, next = node {value = 5}}}}}
    assertEqual(toArray(reverseList(head)), [5, 4, 3, 2, 1])

@Test
fun testTwoElements():
    head = node {value = 1, next = node {value = 2}}
    assertEqual(toArray(reverseList(head)), [2, 1])

@Test
fun testEmpty():
    assertEqual(reverseList(none) == none, true)
