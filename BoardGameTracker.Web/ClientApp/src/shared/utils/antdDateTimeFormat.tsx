export const convertToAntdFormat = (input: string): string => {
  return input.replace(/y/g, 'Y').replace(/d/g, 'D');
}