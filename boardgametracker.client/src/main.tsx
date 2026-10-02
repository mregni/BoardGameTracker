import "./index.css";
import "./utils/i18n";
import React, { Suspense } from "react";
import ReactDOM from "react-dom/client";
import { Toaster } from "sonner";
import AppContainer from "./App.tsx";
import { classConfig } from "./config/sonner.ts";

ReactDOM.createRoot(document.getElementById("root")!).render(
	<React.StrictMode>
		<Suspense>
			<AppContainer />
			<Toaster toastOptions={{ unstyled: true, classNames: classConfig }} />
		</Suspense>
	</React.StrictMode>,
);
