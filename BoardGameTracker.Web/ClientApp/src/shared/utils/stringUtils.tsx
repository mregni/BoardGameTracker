export function decodeHtmlEntities(text: string): string {
  const parser = new DOMParser();
  const decoded = parser.parseFromString(text, "text/html").documentElement.textContent;
  return decoded || "";
}

export function limitStringLength(inputString: string, maxLength = 20): string {
  if (inputString.length <= maxLength) {
      return inputString;
  } else {
      return inputString.slice(0, maxLength - 3) + '...';
  }
}