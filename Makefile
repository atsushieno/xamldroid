VERSION=0.1
DISTNAME=xamldroid-$(VERSION)
CSCOMPILE=dmcs
CSCOMPILE_FLAGS=-debug
RUNTIME=mono
RUNTIME_FLAGS=--debug

SOURCES= xamldroid.cs xamldroid-generator.cs
OTHERFILES= Makefile README
DIST_DEPS = MonoDroid.Xaml.dll
OUTCOME = Mono.Android.Xaml.dll

all: $(OUTCOME)

xamldroid-generator.exe: xamldroid-generator.cs
	$(CSCOMPILE) $(CSCOMPILE_FLAGS) xamldroid-generator.cs

xamldroid.generated.cs : xamldroid-generator.exe Mono.Android.dll
	$(RUNTIME) $(RUNTIME_FLAGS) ./xamldroid-generator.exe Mono.Android.dll

Mono.Android.Xaml.dll : xamldroid.cs xamldroid.generated.cs
	$(CSCOMPILE) $(CSCOMPILE_FLAGS) -debug -t:library -out:Mono.Android.Xaml.dll xamldroid.cs xamldroid.generated.cs -r:Mono.Android.dll -r:MonoDroid.Xaml.dll

clean:
	rm Mono.Android.Xaml.dll Mono.Android.Xaml.dll.mdb xamldroid.generated.cs xamldroid-generator.exe xamldroid-generator.exe.mdb

dist:
	mkdir $(DISTNAME)
	cp $(OUTCOME) $(DIST_DEPS) $(SOURCES) $(OTHERFILES) $(DISTNAME)
	tar jcf $(DISTNAME).tar.bz2 $(DISTNAME)

cleanup-dist:
	rm -rf $(DISTNAME).tar.bz2 $(DISTNAME)

