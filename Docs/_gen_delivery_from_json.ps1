$ErrorActionPreference = 'Stop'
$jsonPath = 'c:\Users\22734\Desktop\unity\CandyCrush\Docs\_delivery_content.json'
$outDesk = Join-Path $env:USERPROFILE 'Desktop\程序开发二测_架构设计线_交付说明.docx'
$outDocs = 'c:\Users\22734\Desktop\unity\CandyCrush\Docs\程序开发二测_架构设计线_交付说明.docx'
$outEn   = 'c:\Users\22734\Desktop\unity\CandyCrush\Docs\Exam2_Architecture_Delivery.docx'

Get-Process WINWORD -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 400

$data = Get-Content -LiteralPath $jsonPath -Encoding UTF8 -Raw | ConvertFrom-Json

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$doc = $word.Documents.Add()
$sel = $word.Selection

function Write-Heading([string]$text, [int]$level = 1) {
  $size = switch ($level) { 1 { 16 } 2 { 14 } default { 12 } }
  $sel.Font.Bold = $true
  $sel.Font.Size = $size
  $sel.TypeText($text)
  $sel.TypeParagraph()
  $sel.Font.Bold = $false
  $sel.Font.Size = 10.5
}
function Write-Para([string]$text) {
  if ([string]::IsNullOrEmpty($text)) { return }
  $sel.TypeText($text)
  $sel.TypeParagraph()
}
function Write-TableObj($t) {
  if ($null -eq $t) { return }
  $rows = [int]$t.rows; $cols = [int]$t.cols
  $cells = @($t.cells)
  $table = $doc.Tables.Add($sel.Range, $rows, $cols)
  $table.Borders.Enable = $true
  $i = 0
  for ($r = 1; $r -le $rows; $r++) {
    for ($c = 1; $c -le $cols; $c++) {
      $table.Cell($r, $c).Range.Text = [string]$cells[$i]
      $i++
    }
  }
  $end = $doc.Content
  $end.Collapse(0)
  $sel.SetRange($end.End, $end.End)
  $sel.TypeParagraph()
}
function Write-Block($block, [int]$baseLevel) {
  if ($null -eq $block) { return }
  if ($block.h) { Write-Heading ([string]$block.h) $baseLevel }
  foreach ($x in @($block.p)) { Write-Para ([string]$x) }
  foreach ($t in @($block.tables)) { Write-TableObj $t }
  foreach ($x in @($block.p2)) { Write-Para ([string]$x) }
  foreach ($t in @($block.tables2)) { Write-TableObj $t }
  foreach ($x in @($block.p3)) { Write-Para ([string]$x) }
  foreach ($t in @($block.tables3)) { Write-TableObj $t }
  foreach ($h2 in @($block.h2)) { Write-Block $h2 ($baseLevel + 1) }
  foreach ($h3 in @($block.h3)) { Write-Block $h3 ($baseLevel + 1) }
}

Write-Heading ([string]$data.title) 1
foreach ($x in @($data.intro)) { Write-Para ([string]$x) }
foreach ($sec in @($data.sections)) { Write-Block $sec 1 }

$tmp = Join-Path $env:TEMP 'CandyCrush_Exam2_Delivery.docx'
if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp -Force }
# SaveAs2(FileName, FileFormat) — 12 = wdFormatXMLDocument (.docx)
$doc.SaveAs2($tmp, 12)
$doc.Close($false)
$word.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($sel) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($doc) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
[GC]::Collect(); [GC]::WaitForPendingFinalizers()

foreach ($p in @($outDesk, $outDocs, $outEn)) {
  if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Force }
  Copy-Item -LiteralPath $tmp -Destination $p -Force
}
Get-Item -LiteralPath $outDesk, $outDocs, $outEn | Format-Table FullName, Length, LastWriteTime
