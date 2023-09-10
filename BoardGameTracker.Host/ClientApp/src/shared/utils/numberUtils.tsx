export function roundToDecimals(value: number | null, decimals: number): number {
  if(value === null){
    return 0;
  }
  
  return parseFloat(value.toFixed(decimals))
}