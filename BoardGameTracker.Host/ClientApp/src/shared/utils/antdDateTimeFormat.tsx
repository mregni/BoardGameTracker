export const convertToAntdFormat = (input: string |undefined): string => {
  if(input === undefined) {
    return '';
  }
  
  return (input as string).replace(/y/g, 'Y').replace(/d/g, 'D');
}