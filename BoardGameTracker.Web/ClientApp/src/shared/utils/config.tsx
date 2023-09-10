export interface CustomWindow extends Window {
  runConfig: Config
}

export interface Config {
  backendUrl: string;
}
