// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.

load DMux8Way16.hdl,
output-file DMux8Way16.out,
compare-to DMux8Way16.cmp,
output-list in sel%B2.3.2 a%B1.16.1 b%B1.16.1 c%B1.16.1 d%B1.16.1 e%B1.16.1 f%B1.16.1 g%B1.16.1 h%B1.16.1;

set in 1,
set sel %B000,
eval,
output;