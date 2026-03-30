import { Trans } from "react-i18next";
import { InlineCode } from "./InlineCode";

interface TableColumn {
	key: string;
	translationKey?: boolean;
}

interface TableProps {
	columns: TableColumn[];
	rows: Record<string, string | undefined>[];
	headers: string[];
}

const thClasses =
	"px-4 py-3 text-left text-sm font-semibold text-purple-300 border-b border-white/10";
const tdClasses = "px-4 py-3 text-sm text-slate-300 border-b border-white/10";

export const Table = ({ columns, rows, headers }: TableProps) => (
	<div className="overflow-x-auto rounded-lg border border-white/10">
		<table className="w-full">
			<thead className="bg-slate-800/50">
				<tr>
					{headers.map((header) => (
						<th key={header} className={thClasses}>
							{header}
						</th>
					))}
				</tr>
			</thead>
			<tbody className="divide-y divide-white/5">
				{rows.map((row) => (
					<tr
						key={row[columns[0].key]}
						className="bg-slate-900/50 hover:bg-slate-800/50 transition-colors"
					>
						{columns.map((col) => (
							<td key={col.key} className={tdClasses}>
								{col.translationKey && row[col.key] ? (
									<Trans
										i18nKey={row[col.key]}
										components={{ code: <InlineCode /> }}
									/>
								) : (
									<code className="text-purple-300">{row[col.key]}</code>
								)}
							</td>
						))}
					</tr>
				))}
			</tbody>
		</table>
	</div>
);
