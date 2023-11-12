export const mapObjectToArray = <T extends Record<string, any>>(obj: T): { name: string; value: any }[] =>
    Object.keys(obj).map(key => ({ name: key, value: obj[key] }));