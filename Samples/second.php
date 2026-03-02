<?php

function test_operators(int $a, int $b): void
{
    // Arithmetic
    print("ADD: ");
    print($a + $b);
    print("\n");

    print("SUB: ");
    print($a - $b);
    print("\n");

    print("MUL: ");
    print($a * $b);
    print("\n");

    print("DIV: ");
    print($a / $b);
    print("\n");

    print("MOD: ");
    print($a % $b);
    print("\n");

    // Comparison
    print("LT: ");
    print($a < $b);
    print("\n");

    print("GT: ");
    print($a > $b);
    print("\n");

    print("LTE: ");
    print($a <= $b);
    print("\n");

    print("GTE: ");
    print($a >= $b);
    print("\n");

    print("SPACESHIP: ");
    print($a <=> $b);
    print("\n");

    // Equality
    print("LOOSE_EQ: ");
    print($a == $b);
    print("\n");

    print("LOOSE_NEQ: ");
    print($a != $b);
    print("\n");

    print("STRICT_EQ: ");
    print($a === $b);
    print("\n");

    print("STRICT_NEQ: ");
    print($a !== $b);
    print("\n");

    // Logical
    print("AND: ");
    print($a && $b);
    print("\n");

    print("OR: ");
    print($a || $b);
    print("\n");

    // Bitwise
    print("BITWISE_AND: ");
    print($a & $b);
    print("\n");

    print("BITWISE_OR: ");
    print($a | $b);
    print("\n");

    print("BITWISE_XOR: ");
    print($a ^ $b);
    print("\n");

    print("SHIFT_LEFT: ");
    print($a << $b);
    print("\n");

    print("SHIFT_RIGHT: ");
    print($a >> $b);
    print("\n");
}

