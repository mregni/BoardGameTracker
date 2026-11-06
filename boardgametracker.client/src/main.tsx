import "./index.css";
import "./utils/i18n";
import React, { Suspense } from "react";
import ReactDOM from "react-dom/client";
import { Toaster } from "sonner";
import AppContainer from "./App.tsx";
import { classConfig } from "./config/sonner.ts";
import { reportReactError } from "./utils/reactErrorReporter.ts";

ReactDOM.createRoot(document.getElementById("root")!, { onCaughtError: reportReactError }).render(
	<React.StrictMode>
		<Suspense>
			<AppContainer />
			<Toaster
				mobileOffset={{ bottom: "calc(var(--bottom-nav-h) + 1rem)" }}
				toastOptions={{ unstyled: true, classNames: classConfig }}
			/>
		</Suspense>
	</React.StrictMode>,
);
