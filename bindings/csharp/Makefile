# SPDX-License-Identifier: MPL-2.0
#
# make test    builds the shared library and runs the C# tests against it
# make example builds the shared library and runs Example against it

ROOT := $(abspath ../..)
LIBDIR := $(ROOT)/build-shared

.PHONY: lib test example clean

lib:
	cmake -B $(LIBDIR) -S $(ROOT) -DCMAKE_BUILD_TYPE=Release -DSIGIL_BUILD_TESTS=OFF -DSIGIL_BUILD_SHARED=ON
	cmake --build $(LIBDIR)

test: lib
	SIGIL_LIB_DIR=$(LIBDIR) dotnet test Sigil.Tests

example: lib
	SIGIL_LIB_DIR=$(LIBDIR) dotnet run --project Example -- $(ROM)

clean:
	rm -rf Sigil/bin Sigil/obj Sigil.Tests/bin Sigil.Tests/obj Example/bin Example/obj
