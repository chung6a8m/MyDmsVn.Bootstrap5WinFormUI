# Bootstrap-like WinForms UI Framework

> Nhìn cách bạn làm mình nhận ra là mình đang nhảy lung tung theo ý thích. Vậy với ý tưởng trên, bạn giúp tôi tạo PRD.md, Plans.md (top down) để mình giao cho AI-Agent nhé! Chú ý là mình muốn nó chạy được cho cả .NET Framework 4.8, với icon ngoài svg, có thể sử dụng [FontAwesome.Sharp](https://www.nuget.org/packages/FontAwesome.Sharp), hoặc font Segoe MDL2 Assets

Đúng. Ở thời điểm này mình nghĩ không nên tiếp tục viết từng control riêng lẻ nữa. Nên dừng lại, thiết kế một bộ UI framework nhỏ có kiến trúc rõ ràng rồi giao AI-Agent triển khai theo PRD.md + Plans.md.

Mình đã đối chiếu lại với Bootstrap 5.3, đặc biệt các nguyên tắc về Accordion/Collapse, Spinner và Progress. Bootstrap 5.3 cũng dùng transition cho Collapse, spinner có border/grow và hỗ trợ dùng trong button; Progress 5.3 đã điều chỉnh cấu trúc accessibility của role="progressbar" và các aria-*. 
B
Bootstrap
+2

Dưới đây là hai file mình đề xuất đưa thẳng vào repo.

PRD.md
# PRD — Bootstrap-like WinForms UI Framework

## 1. Overview

Xây dựng một UI component framework cho Windows Forms theo phong cách
Bootstrap 5.3 nhưng được implement hoàn toàn bằng native WinForms/C#.

Framework phải:

- Chạy được trên .NET Framework 4.8.
- Có thể chạy trên .NET hiện đại nếu project hỗ trợ.
- Không phụ thuộc browser/WebView.
- Không phụ thuộc Bootstrap CSS/JS runtime.
- Hỗ trợ Light/Dark theme.
- Có Bootstrap 5-inspired color palette.
- Có animation infrastructure dùng chung.
- Có icon abstraction hỗ trợ SVG, FontAwesome.Sharp và Segoe MDL2 Assets.
- Có DPI scaling tốt.
- Hỗ trợ Designer của WinForms ở mức hợp lý.
- Có API dễ sử dụng và nhất quán.

Mục tiêu không phải clone 100% Bootstrap CSS.

Mục tiêu là xây dựng một bộ control native WinForms có:
- visual language giống Bootstrap 5.3;
- API quen thuộc;
- theme tập trung;
- animation nhất quán;
- code maintainable.

---

# 2. Goals

## 2.1 Primary Goals

### Theme

Có một AppTheme trung tâm:

- Light
- Dark

Theme phải quản lý:

- Primary
- Secondary
- Success
- Danger
- Warning
- Info
- Light
- Dark
- Body background
- Surface
- Surface secondary
- Border
- Text
- Muted text
- Disabled
- Focus
- Hover
- Active

Theme phải có khả năng:

```csharp
AppTheme.Mode = AppThemeMode.Dark;


và toàn bộ control cập nhật.

2.2 Bootstrap-like palette

Palette mặc định lấy cảm hứng từ Bootstrap 5.3.

Không hard-code màu trực tiếp trong từng control.

Ví dụ:

AppTheme.Colors.Primary
AppTheme.Colors.Success
AppTheme.Colors.Danger


Control chỉ được truy cập theme abstraction.

3. Target Framework
Required

.NET Framework 4.8.

Code phải tránh API chỉ có trên .NET Core/.NET 5+ nếu không có compatibility abstraction.

Đặc biệt không mặc định sử dụng:

Math.Clamp
newer C#/.NET-only APIs
nullable annotations bắt buộc runtime
newer System.Drawing APIs không có trên .NET Framework 4.8

Nếu muốn hỗ trợ .NET hiện đại, compatibility layer phải được thiết kế riêng.

4. Architecture

Framework được chia thành các layer:

BootstrapWinForms
│
├── Theme
│   ├── AppTheme
│   ├── ThemeColors
│   ├── ThemeManager
│   └── ThemeChangedEvent
│
├── Animation
│   ├── BootstrapAnimation
│   ├── BootstrapLoopAnimation
│   ├── BootstrapEasing
│   └── AnimationManager
│
├── Icons
│   ├── IIconProvider
│   ├── SvgIconProvider
│   ├── FontAwesomeIconProvider
│   ├── Mdl2IconProvider
│   └── IconRenderer
│
├── Controls
│   ├── BootstrapButton
│   ├── BootstrapButtonGroup
│   ├── BootstrapButtonToolbar
│   ├── BootstrapTextBox
│   ├── BootstrapCard
│   ├── BootstrapSidebar
│   ├── BootstrapDataGridView
│   ├── BootstrapCollapse
│   ├── BootstrapAccordion
│   ├── BootstrapAccordionHeader
│   ├── BootstrapSpinner
│   └── BootstrapProgressBar
│
└── Utilities
    ├── GraphicsHelper
    ├── ColorHelper
    ├── DpiHelper
    ├── RoundedRectangleHelper
    └── DoubleBufferHelper

5. Theme System
5.1 AppTheme

AppTheme là singleton/static service hoặc root theme object.

API tối thiểu:

AppTheme.Mode
AppTheme.Colors
AppTheme.Fonts
AppTheme.Metrics


Ví dụ:

AppTheme.Mode = AppThemeMode.Dark;

5.2 Theme change

Controls không được polling theme.

Theme phải phát event:

AppTheme.ThemeChanged


Các control subscribe/unsubscribe đúng lifecycle.

Không tạo memory leak.

6. Metrics

Theme phải quản lý các metrics dùng chung:

BorderRadius
SmallBorderRadius
LargeBorderRadius

BorderWidth

ControlHeight
SmallControlHeight
LargeControlHeight

PaddingXS
PaddingSM
PaddingMD
PaddingLG
PaddingXL

SpacingXS
SpacingSM
SpacingMD
SpacingLG
SpacingXL

FocusBorderWidth


Mục tiêu:

Các control không tự định nghĩa hàng chục magic numbers.

7. Animation System

Animation phải là infrastructure dùng chung.

Không cho từng control tự tạo Timer riêng nếu có thể sử dụng animation primitive.

7.1 BootstrapAnimation

One-shot animation:

0 → 1


API:

Start(from, to, duration)
Stop()
Cancel()
Restart(...)


Có:

BootstrapEasing.Linear
BootstrapEasing.EaseIn
BootstrapEasing.EaseOut
BootstrapEasing.EaseInOut


Event:

ProgressChanged
Completed

7.2 BootstrapLoopAnimation

Loop:

0 → 1 → 0 → 1 → ...


Dùng cho:

Spinner
Indeterminate progress
Animated stripes
Future skeleton/loading components
7.3 Reduced motion

Framework nên có:

AppTheme.ReduceMotion


Khi true:

giảm duration;
hoặc disable non-essential animation.

Không được để animation chạy vô hạn khi control invisible/disposed.

8. Icon System
8.1 Requirement

Icon không được hard-code dependency vào một thư viện duy nhất.

Control phải sử dụng abstraction:

IIconProvider


hoặc:

BootstrapIcon

9. Supported Icon Sources

Framework phải hỗ trợ ít nhất:

SVG

SVG ngoài:

Assets/
    Icons/
        add.svg
        edit.svg
        delete.svg
        chevron-down.svg


SVG được load/render thành bitmap hoặc trực tiếp bằng renderer phù hợp.

Không bắt buộc embed SVG vào code.

FontAwesome.Sharp

Có thể optional-reference:

FontAwesome.Sharp


Người dùng có thể truyền icon:

IconProvider = FontAwesomeIconProvider


Không được bắt buộc toàn framework phải phụ thuộc FontAwesome.Sharp.

Package:

https://www.nuget.org/packages/FontAwesome.Sharp

Segoe MDL2 Assets

Hỗ trợ icon từ:

Segoe MDL2 Assets


Ví dụ thông qua Unicode glyph.

Không hard-code glyph trong từng control.

Có abstraction:

Mdl2Icon.Save
Mdl2Icon.Delete
Mdl2Icon.Settings

10. BootstrapButton

Button phải hỗ trợ:

Variant
Outline
Size
Icon
IconPosition
BorderRadius
Hover
Pressed
Disabled
Focus
Loading
LoadingText
LoadingSpinner
Theme

Ví dụ:

var button = new BootstrapButton
{
    Text = "Save",
    Variant = BootstrapButtonVariant.Primary
};

11. BootstrapButton Loading

API:

button.Loading = true;


Khi Loading:

button disabled;
không nhận click;
giữ nguyên kích thước;
hiển thị spinner;
có thể hiển thị LoadingText;
spinner lấy màu phù hợp với button variant.

Ví dụ:

┌────────────────────────┐
│    ◌  Saving...        │
└────────────────────────┘


API:

button.LoadingText = "Saving...";


Có thể:

button.LoadingText = "";


để chỉ hiển thị spinner.

12. BootstrapButtonGroup

Hỗ trợ:

Horizontal
Vertical
Button selection
Single selection
Multiple selection
Connected borders
Radius xử lý theo vị trí button

Ví dụ:

[ New ][ Edit ][ Delete ]

13. BootstrapButtonToolbar

Là container của nhiều ButtonGroup.

Hỗ trợ:

GroupSpacing
Horizontal
Vertical
Left
Center
Right
SpaceBetween
AutoSize

Ví dụ:

[ New ][ Edit ][ Delete ]       [ Refresh ][ Export ]


Toolbar không quản lý selection logic của ButtonGroup.

14. BootstrapTextBox

Hỗ trợ:

Border radius
Border
Focus state
Placeholder
Validation state
Disabled
ReadOnly
Password
Icon left/right
Clear button optional
Theme

Validation:

ValidationState = BootstrapValidationState.Error;

15. BootstrapCard

Hỗ trợ:

Border radius
Shadow
Header
Body
Footer
Padding
Theme
Optional border

API hướng tới:

BootstrapCard
├── Header
├── Body
└── Footer

16. BootstrapSidebar

Hỗ trợ:

Width
Collapsed width
Expand/collapse
Navigation item
Icon
Selected item
Hover
Badge
Dark/Light theme
Animation

Sidebar phải sử dụng BootstrapCollapse/BootstrapAnimation khi phù hợp.

17. BootstrapDataGridView

Là wrapper/subclass của DataGridView.

Mục tiêu:

Theme-aware
Header styling
Alternating rows
Selected row
Hover row
Border
Font
Dark mode
Scrollbar integration nếu có thể
Empty state
Loading overlay

Không phá vỡ API DataGridView chuẩn.

18. BootstrapCollapse

Là animation/layout primitive cho nội dung có thể expand/collapse.

API:

Expanded
Toggle()
Expand()
Collapse()
AnimationDuration
ExpandedHeight


Phải hỗ trợ:

Vertical collapse
Content height measurement
Animation
Reduced motion
Resize handling

ExpandedHeight phải hỗ trợ:

Auto
Fixed
Measured


Không được phụ thuộc Accordion.

19. BootstrapAccordion

Accordion phải được xây dựng trên BootstrapCollapse.

Không duplicate collapse animation.

Architecture:

BootstrapAccordion
    └── BootstrapAccordionItem
          ├── BootstrapAccordionHeader
          └── BootstrapCollapse


Hỗ trợ:

Single open
Multiple open
Flush
Header icon
Chevron
Chevron rotation
AnimationDuration
Theme
20. BootstrapAccordionHeader

Header phải hỗ trợ:

Text
Icon
Chevron
Chevron rotation
Hover
Active
Focus
Selected/Expanded state

Chevron không được phụ thuộc vào browser.

Có thể render bằng:

SVG
FontAwesome.Sharp
Segoe MDL2 Assets

Mặc định ưu tiên SVG nội bộ nhỏ hoặc vector drawing.

21. BootstrapSpinner

Hỗ trợ:

Border
Grow


Properties:

SpinnerSize
Variant
CustomColor
AnimationDuration
Spinning


Không tự quản lý Timer.

Sử dụng:

BootstrapLoopAnimation

22. BootstrapProgressBar

Hỗ trợ:

Minimum
Maximum
Value
Percentage
Variant
CustomColor
BorderRadius
ShowText
TextFormat
Striped
Animated
AnimationDuration
Indeterminate
AnimateTo()

Ví dụ:

progress.Value = 50;


hoặc:

progress.AnimateTo(100);


Indeterminate:

progress.Indeterminate = true;

23. Accessibility

Mặc dù đây là WinForms, framework phải cố gắng cung cấp:

AccessibleName
AccessibleDescription
AccessibleRole
Keyboard focus
Enter/Space activation
Disabled state
Logical state reporting

Spinner phải có AccessibleName/Description phù hợp khi được sử dụng độc lập.

24. DPI

Framework phải hỗ trợ DPI scaling.

Không hard-code pixel assumptions.

Các metrics nên đi qua:

DpiHelper.Scale(...)


Control phải hoạt động tốt ở:

100%
125%
150%
175%
200%
25. Graphics

Tất cả custom-painted controls phải:

Enable double buffering
AntiAlias khi cần
Dispose Pen/Brush/Font/GraphicsPath
Không tạo object nặng trong mỗi frame nếu có thể tránh
Không tạo GDI leak
26. Performance

Không được:

tạo Timer liên tục;
tạo hàng trăm Timer cho hàng trăm control nếu có thể dùng manager;
invalidate toàn form cho animation nhỏ;
tạo bitmap mỗi frame;
leak event handlers.

Animation chỉ chạy khi:

Visible == true
IsHandleCreated == true
Enabled/Running == true


và phải stop khi Dispose.

27. Designer compatibility

Controls phải có:

Parameterless constructor
Public properties
Category attributes nếu hữu ích
Description attributes
DefaultValue attributes khi hợp lý

Không để Designer crash nếu theme chưa initialized.

28. Coding Standards

Target minimum:

.NET Framework 4.8


C# syntax có thể dùng mức phù hợp với compiler/project.

Không dùng API mới hơn target runtime nếu không có compatibility helper.

Nullable reference types không phải requirement.

29. Public API stability

Tên property phải nhất quán.

Ví dụ:

Variant
Size
AnimationDuration
BorderRadius
CustomColor
Loading
Visible


Không tạo nhiều alias chỉ vì convenience.

Nếu API cần thay đổi, ưu tiên clean API trước khi release.

30. Testing

Mỗi control phải có test/demo form.

Tối thiểu:

ThemeDemo
ButtonDemo
ButtonGroupDemo
ButtonToolbarDemo
TextBoxDemo
CardDemo
SidebarDemo
DataGridDemo
CollapseDemo
AccordionDemo
SpinnerDemo
ProgressDemo


Có:

Light
Dark
DPI
Hover
Disabled
Focus
Loading
Animation

31. Definition of Done

Một component chỉ hoàn thành khi:

Build được .NET Framework 4.8.
Không có compile error.
Không có GDI/resource leak rõ ràng.
Theme Light hoạt động.
Theme Dark hoạt động.
Hover hoạt động.
Disabled hoạt động.
Focus hoạt động.
DPI hoạt động.
Animation có thể stop/dispose.
API public có documentation XML cơ bản.
Có demo.
Không duplicate logic đã tồn tại trong primitive/base component.
32. Non-goals

Không cố clone:

Bootstrap CSS pixel-perfect.
Bootstrap JavaScript.
Browser layout engine.
CSS selector system.
Full Bootstrap utility classes.

Đây là:

"Bootstrap-inspired native WinForms UI framework"

không phải Bootstrap port.


---

# `Plans.md`

File này mình cố tình viết **top-down**, để AI-Agent không tự tiện nhảy vào làm `Spinner`, rồi `Accordion`, rồi quay lại sửa Theme như chúng ta vừa làm.

```md
# Implementation Plan — Bootstrap WinForms UI Framework

## 0. Guiding Rule

KHÔNG implement control theo thứ tự ý thích.

Luôn đi từ:

```text
Architecture
    ↓
Theme
    ↓
Infrastructure
    ↓
Base rendering/utilities
    ↓
Primitive controls
    ↓
Composite controls
    ↓
Integration
    ↓
Demo
    ↓
Testing


Một phase chỉ được bắt đầu khi phase trước build ổn định.

Phase 1 — Repository / Project Structure
Objective

Tạo structure sạch cho framework.

/src
    /BootstrapWinForms
        /Theme
        /Animation
        /Icons
        /Controls
        /Utilities

/tests
    /BootstrapWinForms.Tests

/demo
    /BootstrapWinForms.Demo


Nếu project hiện tại đã tồn tại, KHÔNG tạo project duplicate.

Tasks
Xác định target framework hiện tại.
Đảm bảo .NET Framework 4.8 build.
Chuẩn hóa namespaces.
Tạo folder structure.
Xác định coding convention.
Xác định public/internal boundary.
Acceptance
build succeeds


Không có functional control nào được implement ở phase này.

Phase 2 — Theme Foundation
Objective

Hoàn thiện AppTheme trước mọi control.

Implement
AppTheme
AppThemeMode
ThemeColors
ThemeMetrics
ThemeFonts
ThemeChangedEvent

Required

Light palette:

Primary
Secondary
Success
Danger
Warning
Info
Light
Dark
Body
Surface
SurfaceSecondary
Border
Text
Muted
Disabled
Focus
Hover
Active


Dark palette tương ứng.

API
AppTheme.Mode
AppTheme.Colors
AppTheme.Metrics
AppTheme.ThemeChanged

Acceptance

Có ThemeDemo:

[ Light ] [ Dark ]


và màu UI thay đổi.

Phase 3 — Graphics / DPI Utilities
Objective

Tạo toàn bộ utility trước khi custom paint control.

Implement
DpiHelper
GraphicsHelper
ColorHelper
RoundedRectangleHelper
ControlDoubleBufferHelper

Requirements

Rounded rectangle:

GraphicsPath CreateRoundedRectangle(...)


DPI:

Scale(...)
ScaleSize(...)
ScalePadding(...)

Acceptance

Unit/manual test:

100%
125%
150%
200%
Phase 4 — Icon Infrastructure
Objective

Tách icon khỏi controls.

Implement
IIconProvider
IconDescriptor
IconSource
IconRenderer
SvgIconProvider
Mdl2IconProvider


FontAwesome.Sharp integration phải optional.

Không để core project bắt buộc reference FontAwesome.Sharp nếu không cần.

SVG

Hỗ trợ external SVG.

Thiết kế API để sau này có thể thay renderer.

Ví dụ:

IconDescriptor.FromSvg(...)
IconDescriptor.FromFont(...)
IconDescriptor.FromMdl2(...)

Acceptance

Demo:

[ + Add ] [ Edit ] [ Delete ] [ Settings ]


với ít nhất 2 icon source.

Phase 5 — Animation Infrastructure
Objective

Đây là nền móng cho Collapse, Spinner, Progress và Loading.

Implement
BootstrapAnimation
BootstrapLoopAnimation
BootstrapEasing


Nếu cần:

AnimationManager

Rules

Không tạo animation bằng Thread.Sleep.

Không dùng Task.Delay để render frame.

Animation phải chạy UI-thread safe.

Acceptance

Demo:

0 → 100


với:

Linear
EaseOut
EaseInOut

và loop animation.

Test:

Start
Stop
Restart
Dispose
Control invisible

---

# Phase 6 — Base Control Infrastructure

## Objective

Tạo base abstraction dùng chung.

Có thể:

```text
BootstrapControl
BootstrapContainerControl


nhưng KHÔNG over-engineer.

Common responsibilities
Theme subscription
DPI
double buffering
InvalidateTheme()
Dispose event subscription
common colors
common metrics
Acceptance

Tạo dummy control sử dụng ThemeChanged mà không leak.

Phase 7 — BootstrapButton
Objective

Hoàn thiện Button trước Group/Toolbar.

Implement
Variant
Outline
ButtonSize
Icon
IconPosition
BorderRadius
Hover
Pressed
Disabled
Focus

Rendering

States:

Normal
Hover
Pressed
Focused
Disabled

Acceptance

ButtonDemo:

tất cả variants;
outline;
icon;
disabled;
hover;
dark theme.
Phase 8 — BootstrapButton Loading
Objective

Tích hợp Spinner vào Button nhưng theo đúng dependency order.

Rule

Nếu Spinner chưa hoàn thành infrastructure,
có thể dùng temporary renderer nội bộ.

Nhưng trước khi merge phải refactor về:

BootstrapButton
    ↓
BootstrapSpinner
    ↓
BootstrapLoopAnimation

API
Loading
LoadingText

Requirements
Disable interaction.
Preserve button size.
Center spinner + text.
Variant-aware spinner color.
No layout jump.
Acceptance
Save
 ↓
◌ Saving...
 ↓
Save

Phase 9 — BootstrapButtonGroup
Objective

Composite Button.

Implement
Horizontal
Vertical
SelectionMode
Single
Multiple
Connected borders
First/middle/last radius

Dependency
BootstrapButton


Không duplicate button rendering.

Acceptance

ButtonGroupDemo.

Phase 10 — BootstrapButtonToolbar
Objective

Container của ButtonGroup.

Implement
GroupSpacing
Orientation
Alignment
AutoSizeToolbar
CenterVertically
CenterHorizontally


Optional:

SpaceBetween

Dependency
BootstrapButtonGroup


Toolbar không được quản lý selection state của buttons.

Phase 11 — BootstrapTextBox
Objective

Form primitive.

Implement
Placeholder
Border
Focus
Radius
Validation
Icon
Disabled
ReadOnly
Password
Acceptance

TextBoxDemo.

Phase 12 — BootstrapCard
Objective

Surface/container primitive.

Implement
Header
Body
Footer
Border
Shadow
Radius
Padding

Rule

Shadow phải được implement hiệu quả.

Không render shadow bằng hàng chục nested controls.

Phase 13 — BootstrapCollapse
Objective

Đây là layout/animation primitive quan trọng.

Dependency
BootstrapAnimation
DpiHelper

Implement
Expanded
Expand()
Collapse()
Toggle()
AnimationDuration
ExpandedHeight

ExpandedHeight

Thiết kế:

Auto
Measured
Fixed


Ưu tiên:

Measured


cho dynamic content.

Requirements
measure content;
animate Height;
avoid flicker;
handle resize;
stop animation khi disposed;
reduced motion.
Acceptance

CollapseDemo với content variable height.

Phase 14 — BootstrapAccordion
Objective

Accordion phải compose Collapse.

Architecture
BootstrapAccordion
    └── BootstrapAccordionItem
        ├── BootstrapAccordionHeader
        └── BootstrapCollapse

Implement
AllowMultipleOpen
Flush
AnimationDuration

Behavior

Single mode:

A open
B click
A closes
B opens


Multiple mode:

A open
B open
A remains open

Acceptance

AccordionDemo.

Phase 15 — BootstrapAccordionHeader
Objective

Polish header.

Implement
Text
Icon
Chevron
Expanded
Hover
Active
Focus


Chevron:

Collapsed → down
Expanded  → up


Animation:

rotate 0 → 180


Có thể dùng SVG hoặc vector rendering.

Không phụ thuộc browser.

Phase 16 — BootstrapSpinner
Objective

Implement Spinner bằng LoopAnimation.

Types
Border
Grow

Properties
SpinnerSize
Variant
CustomColor
AnimationDuration
Spinning

Dependency
BootstrapLoopAnimation
AppTheme

Rule

KHÔNG tạo Timer mới trong Spinner.

Acceptance

SpinnerDemo:

border;
grow;
all variants;
small;
normal;
large;
start/stop;
dark/light.
Phase 17 — BootstrapProgressBar
Objective

Progress control.

Basic
Minimum
Maximum
Value
Percentage

Appearance
Variant
CustomColor
BorderRadius
ShowText
TextFormat

Bootstrap-inspired
Striped
Animated

Animation
AnimateTo(...)


sử dụng:

BootstrapAnimation


KHÔNG tạo Timer riêng.

Indeterminate
Indeterminate


sử dụng:

BootstrapLoopAnimation

Acceptance

ProgressDemo:

0%;
25%;
50%;
100%;
animated;
striped;
indeterminate;
dark theme.
Phase 18 — BootstrapSidebar
Objective

Composite navigation.

Dependency
BootstrapButton
BootstrapCollapse
BootstrapAnimation
Icon infrastructure

Implement
ExpandedWidth
CollapsedWidth
Expand()
Collapse()
Toggle()
SelectedItem
Icon
Badge

Acceptance

SidebarDemo.

Phase 19 — BootstrapDataGridView
Objective

Theme-aware DataGridView.

Implement
header
body
selected row
alternate row
hover
dark theme
fonts
borders
empty state

Optional:

Loading overlay


Loading overlay nên sử dụng:

BootstrapSpinner


không duplicate spinner implementation.

Phase 20 — Global Theme Integration
Objective

Kiểm tra tất cả control khi theme đổi runtime.

Test:

AppTheme.Mode = Light;
AppTheme.Mode = Dark;


Không recreate controls.

Verify
Button
Group
Toolbar
TextBox
Card
Sidebar
DataGrid
Collapse
Accordion
Spinner
Progress

Phase 21 — Demo Application
Objective

Tạo một demo app có navigation.

Structure:

Sidebar
│
├── Theme
├── Buttons
├── Forms
├── Cards
├── Collapse
├── Accordion
├── Loading
├── Progress
└── DataGrid


Mỗi page phải show:

Light
Dark
hover
disabled
animation
Phase 22 — Testing / Stability
Build

Bắt buộc:

.NET Framework 4.8

Manual tests
100% DPI
125%
150%
175%
200%
Animation tests
start
stop
restart
dispose
hidden
rapidly toggled
Theme tests
Light → Dark
Dark → Light
multiple controls
controls created after theme switch
Resource tests

Kiểm tra:

GDI handles
Timer disposal
event unsubscribe
Bitmap disposal
Pen disposal
Brush disposal
GraphicsPath disposal
Phase 23 — API Review

Trước release:

Kiểm tra naming consistency.

Không cho tồn tại:

AnimationDuration
AnimationTime
Duration
TransitionDuration


cho cùng một khái niệm.

Ưu tiên:

AnimationDuration


Tương tự:

BorderRadius
Variant
CustomColor

Phase 24 — Documentation

Mỗi public control có XML documentation.

Mỗi control có README/demo usage.

Ví dụ:

var spinner = new BootstrapSpinner
{
    Variant = BootstrapSpinnerVariant.Primary,
    SpinnerSize = BootstrapSpinnerSize.Small
};

Dependency Graph

Không implement theo thứ tự file.

Implement theo dependency graph:

                    AppTheme
                       │
             ┌─────────┴─────────┐
             │                   │
        Graphics/DPI          Icons
             │                   │
             └─────────┬─────────┘
                       │
                Base Control
                       │
              BootstrapAnimation
                       │
             BootstrapLoopAnimation
                       │
       ┌───────────────┼────────────────┐
       │               │                │
    Spinner       ProgressBar       Collapse
       │               │                │
       │               │           Accordion
       │               │
       └───────┬───────┘
               │
         BootstrapButton
               │
       ┌───────┴────────┐
       │                │
 ButtonGroup       Button Loading
       │
 ButtonToolbar

TextBox ──────┐
Card ─────────┤
Sidebar ──────┤
DataGrid ─────┘

Important AI-Agent Rules
Rule 1

Không tạo implementation duplicate.

Nếu logic đã tồn tại ở:

AppTheme
GraphicsHelper
BootstrapAnimation
BootstrapLoopAnimation
BootstrapCollapse
BootstrapSpinner


thì control mới phải reuse.

Rule 2

Không tự ý đổi public API đã được phê duyệt.

Nếu phát hiện API có vấn đề:

ghi lại;
đề xuất thay đổi;
không âm thầm đổi.
Rule 3

Không sử dụng .NET API không có trong .NET Framework 4.8.

Nếu cần API mới:

create compatibility helper

Rule 4

Không bắt buộc FontAwesome.Sharp trong core.

FontAwesome integration phải optional.

Rule 5

SVG phải là một icon source độc lập.

Không biến SVG thành dependency bắt buộc của mọi control.

Rule 6

Không tạo Timer trong từng control nếu animation primitive đã tồn tại.

Ví dụ:

WRONG:

Spinner → Timer
Progress → Timer
Collapse → Timer
Accordion → Timer


CORRECT:

BootstrapAnimation
BootstrapLoopAnimation
        │
        ├── Spinner
        ├── Progress
        └── Collapse

Rule 7

Custom control phải dispose resource.

Đặc biệt:

Timer
Bitmap
Graphics
GraphicsPath
Pen
Brush
Font
event subscription

Rule 8

Không tối ưu quá sớm.

Ưu tiên:

Correctness
→ Maintainability
→ Visual consistency
→ Performance


Sau đó mới optimize.

Final Acceptance Criteria

Framework được coi là hoàn thành khi:

Build thành công trên .NET Framework 4.8.
Có Light/Dark theme.
Có Bootstrap palette.
Có shared animation infrastructure.
Có icon abstraction.
Có SVG icon support.
Có optional FontAwesome.Sharp integration.
Có Segoe MDL2 Assets integration.
Có BootstrapButton.
Có ButtonGroup.
Có ButtonToolbar.
Có Button Loading.
Có TextBox.
Có Card.
Có Sidebar.
Có DataGridView.
Có Collapse.
Có Accordion.
Có AccordionHeader.
Có Spinner.
Có ProgressBar.
Các animation dùng infrastructure chung.
Không có Timer leak rõ ràng.
Không có GDI resource leak rõ ràng.
Có Demo Application.
Có documentation cơ bản.
Hoạt động ở DPI 100–200%.

---

## Một thay đổi mình rất khuyên AI-Agent làm

Có một điểm trong các đoạn code trước của chúng ta mình **không muốn bê nguyên vào project mới**: các property kiểu `SizeMode`, `SpinnerSize`, `AutoSizeToolbar` và cách mỗi control tự `RefreshTheme()`.

Trong project mới nên chuẩn hóa từ đầu.

Ví dụ Spinner:

```csharp
public BootstrapSpinnerSize SpinnerSize { get; set; }


là hợp lý vì Control.Size đã có sẵn.

Nhưng theme nên đi theo:

AppTheme
   ↓ ThemeChanged
BootstrapControl
   ↓
InvalidateTheme()


thay vì application phải gọi:

spinner.RefreshTheme();
button.RefreshTheme();
card.RefreshTheme();
...


Tương tự, animation phải là một dependency, không phải implementation detail của từng control.

Đây chính là thứ sẽ giúp project không quay lại tình trạng “nghĩ ra control nào làm control đó” như lúc đầu.

Icon cũng nên chốt ngay từ đầu

Mình đề xuất API cuối cùng theo hướng:

button.Icon = BootstrapIcon.Save;


hoặc:

button.Icon = IconDescriptor.FromSvg(
    "Assets/Icons/save.svg");


hoặc:

button.Icon = IconDescriptor.FromFontAwesome(
    FontAwesome.Sharp.IconChar.Save);


hoặc:

button.Icon = IconDescriptor.FromMdl2(
    Mdl2Icon.Save);


Như vậy BootstrapButton không cần biết icon đến từ đâu.

Và một nguyên tắc quan trọng cho AI-Agent

Khi giao hai file này, mình sẽ thêm instruction đầu prompt:

Read PRD.md and Plans.md completely before modifying code. Do not start implementation from the user's latest request. Follow the dependency graph and implementation phases in Plans.md. After each phase, build the .NET Framework 4.8 target and fix errors before proceeding. Do not invent parallel architectures or duplicate existing infrastructure.

Cách này sẽ phù hợp hơn nhiều với mục tiêu của bạn: không phải có vài control đẹp, mà là xây được một mini Bootstrap-like UI framework cho WinForms, có nền móng đủ tốt để sau này thêm Alert, Badge, Toast, Tooltip, Modal, Dropdown, Tabs, Pagination, Skeleton, DatePicker... mà không phải đập kiến trúc cũ đi làm lại.

B
Sources