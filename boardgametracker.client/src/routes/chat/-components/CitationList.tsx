import { useTranslation } from "react-i18next";
import type { RagCitation } from "@/models";
import { CitationImage } from "./CitationImage";

interface Props {
	citations: RagCitation[];
}

export const CitationList = ({ citations }: Props) => {
	const { t } = useTranslation("chat");

	return (
		<div className="flex flex-col gap-2 border-t border-white/10 pt-3">
			<span className="text-xs uppercase tracking-wide text-white/40">{t("sources")}</span>
			{citations.map((citation) => {
				const score = Math.max(0, citation.score);
				return (
					<div key={`${citation.manualId}-${citation.page}`} className="flex flex-col gap-1">
						<div className="flex items-center justify-between gap-2 text-xs">
							<span className="text-white/70 truncate">
								{citation.title || t("untitled-manual")} ·{" "}
								{citation.page != null ? t("page", { page: citation.page }) : t("unknown-page")}
							</span>
							<span className="text-white/40 shrink-0">{Math.round(score * 100)}%</span>
						</div>
						<div className="w-full bg-primary/10 rounded-full h-1.5 overflow-hidden">
							<div className="h-full rounded-full bg-primary transition-all" style={{ width: `${score * 100}%` }} />
						</div>
						{citation.imageUrl && (
							<CitationImage
								url={citation.imageUrl}
								alt={`${citation.title || t("untitled-manual")}${citation.page != null ? ` · ${t("page", { page: citation.page })}` : ""}`}
							/>
						)}
					</div>
				);
			})}
		</div>
	);
};
