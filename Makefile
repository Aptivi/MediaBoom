MODAPI = 3
ROOT_DIR := $(shell dirname "$(realpath $(lastword $(MAKEFILE_LIST)))")

OUTPUT = "$(ROOT_DIR)/private/MediaBoom.Cli/bin/$(ENVIRONMENT)/net10.0"
OUTPUT_QP = "$(ROOT_DIR)/private/MediaBoom.QuickPlay/bin/$(ENVIRONMENT)/net10.0"
OUTPUT_QR = "$(ROOT_DIR)/private/MediaBoom.QuickRadio/bin/$(ENVIRONMENT)/net10.0"
BINARIES = "$(ROOT_DIR)/assets/mediaboom" "$(ROOT_DIR)/assets/mb-radioplay" "$(ROOT_DIR)/assets/mb-sndplay"
MANUALS = "$(ROOT_DIR)/assets/mediaboom.1" "$(ROOT_DIR)/assets/mb-radioplay.1" "$(ROOT_DIR)/assets/mb-sndplay.1"
DESKTOPS = "$(ROOT_DIR)/assets/mediaboom.desktop"
BRANDINGS = "$(ROOT_DIR)/assets/OfficialAppIcon-MediaBoom-512.png"

ARCH := $(shell if [ `uname -m` = "x86_64" ]; then echo "linux-x64"; else echo "linux-arm64"; fi)

ifndef DESTDIR
FDESTDIR := /usr/local
else
FDESTDIR := $(DESTDIR)/usr
endif

ifndef ENVIRONMENT
ENVIRONMENT := Release
endif

.PHONY: all install lite

# General use

all:
	$(MAKE) all-online BUILDARGS="$(BUILDARGS)"

all-online:
	$(MAKE) invoke-build ENVIRONMENT=Release BUILDARGS="$(BUILDARGS)"

dbg:
	$(MAKE) invoke-build ENVIRONMENT=Debug BUILDARGS="$(BUILDARGS)"

dbg-ci:
	$(MAKE) invoke-build ENVIRONMENT=Debug BUILDARGS="-p:ContinuousIntegrationBuild=true $(BUILDARGS)"

rel-ci:
	$(MAKE) invoke-build ENVIRONMENT=Release BUILDARGS="-p:ContinuousIntegrationBuild=true $(BUILDARGS)"

doc: invoke-doc-build

clean:
	adt clean

all-offline:
	$(MAKE) invoke-build-offline BUILDARGS="-p:ContinuousIntegrationBuild=true $(BUILDARGS)"

init-offline:
	$(MAKE) invoke-init-offline

install:
    # Prepare directories
	mkdir -m 755 -p $(FDESTDIR)/bin $(FDESTDIR)/lib/mediaboom-$(MODAPI) $(FDESTDIR)/lib/mediaboom-$(MODAPI)-quickplay $(FDESTDIR)/lib/mediaboom-$(MODAPI)-radioplay $(FDESTDIR)/share/applications $(FDESTDIR)/share/man/man1/
    # Install binaries and manuals
	install -m 755 -t $(FDESTDIR)/bin/ $(BINARIES)
	install -m 644 -t $(FDESTDIR)/share/man/man1/ $(MANUALS)
    # Install application files
	find "$(OUTPUT)" -mindepth 1 -type d -exec sh -c 'mkdir -p -m 755 "$(FDESTDIR)/lib/mediaboom-$(MODAPI)/$$(realpath --relative-to "$(OUTPUT)" "$$0")"' {} \;
	find "$(OUTPUT_QP)" -mindepth 1 -type d -exec sh -c 'mkdir -p -m 755 "$(FDESTDIR)/lib/mediaboom-$(MODAPI)-quickplay/$$(realpath --relative-to "$(OUTPUT_QP)" "$$0")"' {} \;
	find "$(OUTPUT_QR)" -mindepth 1 -type d -exec sh -c 'mkdir -p -m 755 "$(FDESTDIR)/lib/mediaboom-$(MODAPI)-radioplay/$$(realpath --relative-to "$(OUTPUT_QR)" "$$0")"' {} \;
	find "$(OUTPUT)" -mindepth 1 -type f -exec sh -c 'install -m 644 -t "$(FDESTDIR)/lib/mediaboom-$(MODAPI)/$$(dirname $$(realpath --relative-to "$(OUTPUT)" "$$0"))" "$$0"' {} \;
	find "$(OUTPUT_QP)" -mindepth 1 -type f -exec sh -c 'install -m 644 -t "$(FDESTDIR)/lib/mediaboom-$(MODAPI)-quickplay/$$(dirname $$(realpath --relative-to "$(OUTPUT_QP)" "$$0"))" "$$0"' {} \;
	find "$(OUTPUT_QR)" -mindepth 1 -type f -exec sh -c 'install -m 644 -t "$(FDESTDIR)/lib/mediaboom-$(MODAPI)-radioplay/$$(dirname $$(realpath --relative-to "$(OUTPUT_QR)" "$$0"))" "$$0"' {} \;
    # Install desktop and branding files
	install -m 755 -t $(FDESTDIR)/share/applications/ $(DESKTOPS)
	install -m 755 -t $(FDESTDIR)/lib/mediaboom-$(MODAPI)/ $(BRANDINGS)
    # Rename binaries to reflect version
	mv $(FDESTDIR)/bin/mediaboom $(FDESTDIR)/bin/mediaboom-$(MODAPI)
	mv $(FDESTDIR)/share/man/man1/mediaboom.1 $(FDESTDIR)/share/man/man1/mediaboom-$(MODAPI).1
	mv $(FDESTDIR)/share/applications/mediaboom.desktop $(FDESTDIR)/share/applications/mediaboom-$(MODAPI).desktop
    # Replace directories in binary files
	sed -i 's|/usr/lib/mediaboom|/usr/lib/mediaboom-$(MODAPI)|g' $(FDESTDIR)/bin/mediaboom-*
	sed -i 's|/usr/lib/mediaboom|/usr/lib/mediaboom-$(MODAPI)|g' $(FDESTDIR)/bin/mb-*
	sed -i 's|/usr/lib/mediaboom|/usr/lib/mediaboom-$(MODAPI)|g' $(FDESTDIR)/share/applications/mediaboom-$(MODAPI).desktop
	sed -i 's|/usr/bin/mediaboom|/usr/bin/mediaboom-$(MODAPI)|g' $(FDESTDIR)/share/applications/mediaboom-$(MODAPI).desktop
    # Trim unused runtimes
	find '$(FDESTDIR)/lib/' -type d -name "runtimes" -exec sh -c 'find $$0 -mindepth 1 -maxdepth 1 -not -name $(ARCH) -type d -exec rm -rf \{\} \;' {} \;

# Below targets specify functions for full build

invoke-build:
	adt build -b "-p:Configuration=$(ENVIRONMENT) $(BUILDARGS)"
    
invoke-doc-build:
	adt gendocs

invoke-build-offline:
	HOME=`pwd`"/debian/homedir" \
	DOTNET_CLI_HOME=`pwd`"/debian/homedir" \
	adt build -b "-p:Configuration=$(ENVIRONMENT) $(BUILDARGS)"

invoke-init-offline:
	adt vendorize
