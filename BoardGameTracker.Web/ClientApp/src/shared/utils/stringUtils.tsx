export function decodeHtmlEntities(text: string): string {
  const parser = new DOMParser();
  const decoded = parser.parseFromString(text, "text/html").documentElement.textContent;
  return decoded || "";
}