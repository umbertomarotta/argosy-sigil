# SPDX-License-Identifier: MPL-2.0
#
# make test    builds the shared library and runs the C# tests against it
# make example builds the shared library and runs Example against it
#
# The binding targets net462. On Windows that runs on .NET Framework; elsewhere it needs Mono, which finds the
# library through LD_LIBRARY_PATH / DYLD_LIBRARY_PATH rather than SIGIL_LIB_DIR.

ROOT := $(abspath ../..)
LIBDIR := $(ROOT)/build-shared

.PHONY: lib test example clean

lib:
	cmake -B $(LIBDIR) -S $(ROOT) -DCMAKE_BUILD_TYPE=Release -DSIGIL_BUILD_TESTS=OFF -DSIGIL_BUILD_SHARED=ON
	cmake --build $(LIBDIR)

test: lib
	SIGIL_LIB_DIR=$(LIBDIR) LD_LIBRARY_PATH=$(LIBDIR) DYLD_LIBRARY_PATH=$(LIBDIR) dotnet test Sigil.Tests

example: lib
	SIGIL_LIB_DIR=$(LIBDIR) LD_LIBRARY_PATH=$(LIBDIR) DYLD_LIBRARY_PATH=$(LIBDIR) dotnet run --project Example -- $(ROM)

clean:
	rm -rf Sigil/bin Sigil/obj Sigil.Tests/bin Sigil.Tests/obj Example/bin Example/obj
