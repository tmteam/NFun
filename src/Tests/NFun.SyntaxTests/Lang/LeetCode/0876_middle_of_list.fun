# LeetCode 0876 — Middle of the Linked List
#
# Tortoise and hare: when fast reaches end, slow is at the middle.
# For even-length lists return the second middle.

type node = {value: int, next: node? = none}

fun middleValue(head):
    slow = head
    fast = head
    # Loop condition narrows `fast` and `fast.next` to non-none; `slow` is
    # only guaranteed non-none by the algorithm invariant (it advances by 1
    # for every 2 advances of fast), which the type system can't see — keep
    # `!` on slow.next.
    while fast != none and fast.next != none:
        slow = slow!.next
        fast = fast.next.next
    if slow == none: return -1
    return slow.value

@Test
fun testOdd():
    head = node {
        value = 1
        next = node {
            value = 2
            next = node {
                value = 3
                next = node {
                    value = 4
                    next = node {value = 5}
                }
            }
        }
    }
    assertEqual(middleValue(head), 3)

@Test
fun testEven():
    head = node {
        value = 1
        next = node {
            value = 2
            next = node {
                value = 3
                next = node {
                    value = 4
                    next = node {
                        value = 5
                        next = node {value = 6}
                    }
                }
            }
        }
    }
    assertEqual(middleValue(head), 4)
