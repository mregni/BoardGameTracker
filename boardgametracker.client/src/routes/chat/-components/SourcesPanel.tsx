import { useTranslation } from "react-i18next";
import Download from "@/assets/icons/download.svg?react";
import type { RagCitation } from "@/models";
import { downloadManualCall } from "@/services/manualService";
import { PageImage } from "./PageImage";

interface Props {
	citations: RagCitation[];
	focusedIndex: number;
	onFocus: (index: number) => void;
	onExpand: () => void;
}

export const SourcesPanel = ({ citations, focusedIndex, onFocus, onExpand }: Props) => {
	const { t } = useTranslation("chat");

	const focused = citations[focusedIndex] ?? citations[0];
	if (!focused) {
		return null;
	}

	const docName = focused.title || t("untitled-manual");
	const pageLabel = focused.page != null ? t("page", { page: focused.page }) : t("unknown-page");

	const handleDownload = () => {
		void downloadManualCall(focused.manualId, docName);
	};

	return (
		<div className="flex h-full w-full flex-col">
			<div className="flex items-center justify-between gap-3 pb-3">
				<h3 className="text-xs font-bold uppercase tracking-[0.14em] text-white/60">{t("sources")}</h3>
				<button
					type="button"
					onClick={handleDownload}
					title={docName}
					className="inline-flex shrink-0 cursor-pointer items-center gap-1.5 rounded-md border border-white/10 px-2.5 py-1 text-xs font-medium text-white/70 transition-colors hover:border-primary hover:text-white"
				>
					<Download className="size-3.5" />
					{t("download-manual")}
				</button>
			</div>

			<div className="flex gap-2 overflow-x-auto pb-3">
				{citations.map((citation, index) => {
					const isFocused = index === focusedIndex;
					return (
						<button
							type="button"
							key={`${citation.manualId}-${citation.page}`}
							onClick={() => onFocus(index)}
							aria-current={isFocused}
							className="shrink-0 text-center"
						>
							<div
								className={`h-16 w-12 overflow-hidden rounded-md border ${
									isFocused ? "border-transparent ring-2 ring-primary" : "border-white/10 opacity-60"
								}`}
							>
								<PageImage
									url={citation.imageUrl}
									alt=""
									className="h-full w-full object-cover"
									fallback={
										<div className="flex h-full items-center justify-center text-[10px] text-white/40">
											{citation.page ?? "?"}
										</div>
									}
								/>
							</div>
							<span className={`mt-1 block text-[11px] ${isFocused ? "font-bold text-white" : "text-white/40"}`}>
								{citation.page ?? "–"}
							</span>
						</button>
					);
				})}
			</div>

			<div className="flex items-center justify-between gap-2 pb-2 text-xs">
				<span className="font-bold text-white">{pageLabel}</span>
				<span className="text-white/40">{t("click-to-enlarge")}</span>
			</div>

			<button
				type="button"
				onClick={onExpand}
				aria-label={t("click-to-enlarge")}
				className="flex min-h-0 flex-1 cursor-zoom-in items-center justify-center overflow-hidden rounded-lg border border-white/10 bg-black/30 p-3 transition-colors hover:border-primary"
			>
				<PageImage
					key={`${focused.manualId}-${focused.page}`}
					url={focused.imageUrl}
					alt={`${docName} · ${pageLabel}`}
					className="max-h-full max-w-full rounded object-contain"
					fallback={<span className="text-sm text-white/40">{t("image-unavailable")}</span>}
				/>
			</button>
		</div>
	);
};
