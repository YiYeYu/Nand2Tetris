// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.

load And3.hdl,
output-file And3.out,
compare-to And3.cmp,
output-list a b c out;

set a 0,
set b 0,
set c 0,
eval,
output;

set a 0,
set b 1,
set c 0,
eval,
output;

set a 1,
set b 0,
set c 0,
eval,
output;

set a 1,
set b 1,
set c 0,
eval,
output;

set a 1,
set b 1,
set c 1,
eval,
output;
