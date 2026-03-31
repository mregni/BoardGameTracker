import "@radix-ui/themes/styles.css";
import "./index.css";
import "./utils/i18n";
import { Theme } from "@radix-ui/themes";
import React, { Suspense } from "react";
import ReactDOM from "react-dom/client";
import { Toaster } from "sonner";
import AppContainer from "./App.tsx";
import { classConfig } from "./config/sonner.ts";

ReactDOM.createRoot(document.getElementById("root")!).render(
	<React.StrictMode>
		<Suspense>
			<Theme appearance="dark" accentColor="purple" grayColor="gray" panelBackground="solid">
				<Toaster toastOptions={{ unstyled: true, classNames: classConfig }} />
				<AppContainer />
			</Theme>
		</Suspense>
	</React.StrictMode>,
);
