import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import type { RagCitation } from "@/models";
import { PageImage } from "./PageImage";

interface Props {
	citations: RagCitation[];
	index: number;
	onIndexChange: (index: number) => void;
	onClose: () => void;
}

export const SourcesOverlay = ({ citations, index, onIndexChange, onClose }: Props) => {
	const { t } = useTranslation("chat");
	const current = citations[index];
	const lastIndex = citations.length - 1;

	useEffect(() => {
		const onKey = (event: KeyboardEvent) => {
			if (event.key === "Escape") {
				onClose();
			} else if (event.key === "ArrowLeft" && index > 0) {
				onIndexChange(index - 1);
			} else if (event.key === "ArrowRight" && index < lastIndex) {
				onIndexChange(index + 1);
			}
		};
		window.addEventListener("keydown", onKey);
		return () => window.removeEventListener("keydown", onKey);
	}, [index, lastIndex, onClose, onIndexChange]);

	if (!current) {
		return null;
	}

	const docName = current.title || t("untitled-manual");
	const pageLabel = current.page != null ? t("page", { page: current.page }) : t("unknown-page");

	return (
		<div
			role="dialog"
			aria-modal="true"
			aria-label={t("sources")}
			className="fixed inset-0 z-50 flex flex-col bg-black/90"
		>
			<div className="mx-auto mt-3 h-1 w-9 rounded-full bg-white/30" />
			<div className="flex items-center justify-between px-4 pb-2 pt-2">
				<span className="truncate text-sm text-white/70">
					{docName} · {pageLabel}
				</span>
				<button
					type="button"
					onClick={onClose}
					aria-label={t("close")}
					className="grid size-11 shrink-0 cursor-pointer place-items-center"
				>
					<span className="grid size-8 place-items-center rounded-full bg-white/10 text-lg leading-none text-white">
						×
					</span>
				</button>
			</div>

			<div onClick={onClose} className="flex min-h-0 flex-1 cursor-zoom-out items-center justify-center px-3">
				<PageImage
					key={`${current.manualId}-${current.page}`}
					url={current.imageUrl}
					alt={`${docName} · ${pageLabel}`}
					className="max-h-full max-w-full cursor-default rounded object-contain"
					fallback={<span className="text-sm text-white/40">{t("image-unavailable")}</span>}
					onClick={(event) => event.stopPropagation()}
				/>
			</div>

			<div className="flex items-center justify-center gap-6 py-4">
				<button
					type="button"
					onClick={() => onIndexChange(index - 1)}
					disabled={index === 0}
					aria-label={t("previous")}
					className="grid size-11 cursor-pointer place-items-center disabled:cursor-default disabled:opacity-30"
				>
					<span className="grid size-8 place-items-center rounded-full bg-white/10 text-lg leading-none text-white">
						‹
					</span>
				</button>
				<div className="flex items-center gap-1.5">
					{citations.map((citation, dotIndex) => (
						<span
							key={`${citation.manualId}-${citation.page}`}
							className={`h-1.5 rounded-full transition-all ${dotIndex === index ? "w-5 bg-primary" : "w-1.5 bg-white/30"}`}
						/>
					))}
				</div>
				<button
					type="button"
					onClick={() => onIndexChange(index + 1)}
					disabled={index === lastIndex}
					aria-label={t("next")}
					className="grid size-11 cursor-pointer place-items-center disabled:cursor-default disabled:opacity-30"
				>
					<span className="grid size-8 place-items-center rounded-full bg-white/10 text-lg leading-none text-white">
						›
					</span>
				</button>
			</div>
		</div>
	);
};
