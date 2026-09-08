# Rivet Reach - Third-party provenance and notices

Recorded for initialization on 2026-09-08. Rivet Reach remains proprietary under [LICENSE.md](../LICENSE.md); the third-party material below retains its upstream terms and ownership.

## Sources and pinned versions

Starter assets and settings were copied from Unity 6000.4.4f1's bundled `com.unity.template.3d-cross-platform-17.0.14.tgz`, whose package identifier is `com.unity.template.urp-blank`. The changes are recorded in [Unity setup](UNITY_SETUP.md): tutorial material was omitted, project branding/settings adjusted and scene/assets relocated while retaining GUIDs.

Packages are resolved by Unity from [manifest.json](../Packages/manifest.json) and [packages-lock.json](../Packages/packages-lock.json). Their downloaded source/binaries remain in the ignored package cache or Editor installation; this repository records their declarations and copies of the supplied root license/notice files. Notice copies below are preserved as supplied, including the blank third-party placeholder file shipped in the template; those placeholders do not identify additional components.

| Material | Version | Use/provenance | Supplied licenses and notices |
|---|---|---|---|
| `com.unity.template.urp-blank` | 17.0.14 | Copied starter assets/settings | [Unity Companion License notice](licenses/com.unity.template.urp-blank@17.0.14/LICENSE.md); [upstream notice file](licenses/com.unity.template.urp-blank@17.0.14/THIRD_PARTY_NOTICES.md) (unfilled template placeholders) |
| `com.unity.burst` | 1.8.29 | Transitive package | Unity Companion + Package Distribution: [License](licenses/com.unity.burst@1.8.29/LICENSE.md); [Third-party notices](licenses/com.unity.burst@1.8.29/THIRD_PARTY_NOTICES.md) |
| `com.unity.collections` | 6.4.0 | Transitive package | Unity Companion: [License](licenses/com.unity.collections@6.4.0/LICENSE.md) |
| `com.unity.ext.nunit` | 2.0.5 | Transitive package | Unity Package Distribution: [License](licenses/com.unity.ext.nunit@2.0.5/LICENSE.md); [Third-party notices](licenses/com.unity.ext.nunit@2.0.5/THIRD_PARTY_NOTICES.md) |
| `com.unity.ide.visualstudio` | 2.0.27 | Direct package | MIT: [License](licenses/com.unity.ide.visualstudio@2.0.27/LICENSE.md); [Third-party notices](licenses/com.unity.ide.visualstudio@2.0.27/THIRD_PARTY_NOTICES.md) |
| `com.unity.inputsystem` | 1.19.0 | Direct package | Unity Companion: [License](licenses/com.unity.inputsystem@1.19.0/LICENSE.md) |
| `com.unity.mathematics` | 1.3.3 | Transitive package | Unity Companion: [License](licenses/com.unity.mathematics@1.3.3/LICENSE.md) |
| `com.unity.nuget.mono-cecil` | 1.11.6 | Transitive package | Unity Companion: [License](licenses/com.unity.nuget.mono-cecil@1.11.6/LICENSE.md); [Third-party notices](licenses/com.unity.nuget.mono-cecil@1.11.6/THIRD_PARTY_NOTICES.md) |
| `com.unity.render-pipelines.core` | 17.4.0 | Transitive package | Unity Companion: [License](licenses/com.unity.render-pipelines.core@17.4.0/LICENSE.md); [Third-party notices](licenses/com.unity.render-pipelines.core@17.4.0/THIRD_PARTY_NOTICES.md) |
| `com.unity.render-pipelines.universal` | 17.4.0 | Direct package | Unity Companion: [License](licenses/com.unity.render-pipelines.universal@17.4.0/LICENSE.md); [Third-party notices](licenses/com.unity.render-pipelines.universal@17.4.0/THIRD_PARTY_NOTICES.md) |
| `com.unity.render-pipelines.universal-config` | 17.4.0 | Transitive package | Unity Companion: [License](licenses/com.unity.render-pipelines.universal-config@17.4.0/LICENSE.md) |
| `com.unity.searcher` | 4.9.4 | Transitive package | Unity Companion: [License](licenses/com.unity.searcher@4.9.4/LICENSE.md) |
| `com.unity.shadergraph` | 17.4.0 | Transitive package | Unity Companion: [License](licenses/com.unity.shadergraph@17.4.0/LICENSE.md) |
| `com.unity.test-framework` | 1.6.0 | Direct package | Unity Companion: [License](licenses/com.unity.test-framework@1.6.0/LICENSE.md) |
| `com.unity.test-framework.performance` | 3.4.0 | Transitive package | Unity Companion: [License](licenses/com.unity.test-framework.performance@3.4.0/LICENSE.md); [Third-party notices](licenses/com.unity.test-framework.performance@3.4.0/THIRD_PARTY_NOTICES.md) |
| `com.unity.ugui` | 2.0.0 | Direct package | Unity Companion: [License](licenses/com.unity.ugui@2.0.0/LICENSE.md) |

The lockfile also lists Unity built-in modules `hierarchycore`, `imgui`, `jsonserialize`, `physics`, `terrain`, `ui` and `uielements`, each at 1.0.0. These come with the pinned Editor and have no separate root license/notice file in their installed package directory; the Unity Engine terms apply to their use.

## Retaining notices when distributing

The [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) permits authoring/distribution in connection with a valid Unity Engine license, retains ownership of our own game content, and requires its license and associated copyright notice with substantial portions of its material. Keep the linked package/template notices with their respective material. The [Unity Package Distribution License](https://unity.com/legal/licenses/unity-package-distribution-license) restricts distribution of covered work to binaries integrated with project content under its terms; package caches and standalone tool binaries are not part of this repository's distribution.

Included component notices cover MIT, BSD/Zero-Clause BSD, zlib, Apache 2.0 with LLVM exceptions, NCSA and NVIDIA FXAA terms. Preserve applicable copyright, permission, disclaimer and attribution texts when distributing those components. The full supplied notices are retained above rather than replacing them with these summaries. These package declarations do not require publishing Rivet Reach's own gameplay source merely to use them in a Unity game.

At the first player build, identify the components actually shipped and include their required licenses/notices in the release materials; development-only packages need not be described as runtime features. Recheck the inventory when packages or the Editor change. This initial inventory covers the template and resolved packages, not a completed player-build attribution audit.

## First POC additions

The first POC enables the pinned Editor's built-in `audio`, `physics` and `screencapture` modules at 1.0.0; `imageconversion` is resolved transitively. These are Unity Engine modules, with the same engine terms as the previously listed built-ins. No external gameplay or asset package was added. The player build includes the engine-provided Mono/D3D runtime libraries and retains their supplied distribution files.

The player models, skin images, terrain tiles, sky/terrain shaders and synthesized sound effects were created for Rivet Reach from the versioned scripts and code. Blender is an authoring tool; its executable and implementation are not redistributed in the game. No third-party character, texture or audio asset was copied. The Windows build helper includes this notice inventory, the supplied package/template license copies and Rivet Reach's proprietary license alongside the player. Some preserved package notices cover development tooling as well as runtime material; inclusion does not imply those tools are gameplay features.
