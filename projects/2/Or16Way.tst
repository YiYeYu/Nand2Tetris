// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.

load Or16Way.hdl,
output-file Or16Way.out,
compare-to Or16Way.cmp,
output-list in%B2.16.2 out;

set in %B0000000000000000,
eval,
output;

set in %B0000000011111111,
eval,
output;

set in %B0000000000010000,
eval,
output;

set in %B0000000000000001,
eval,
output;

set in %B0000000000100110,
eval,
output;