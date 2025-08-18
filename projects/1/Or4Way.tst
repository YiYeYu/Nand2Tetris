// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.

load Or4Way.hdl,
output-file Or4Way.out,
compare-to Or4Way.cmp,
output-list in%B2.4.2 out;

set in %B0000,
eval,
output;

set in %B1111,
eval,
output;

set in %B0001,
eval,
output;

set in %B0010,
eval,
output;

set in %B0100,
eval,
output;

set in %B1100,
eval,
output;

set in %B0110,
eval,
output;

set in %B1001,
eval,
output;