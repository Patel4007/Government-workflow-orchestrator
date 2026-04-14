import { Card, CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Button } from "../components/ui/button";
import { FileText, Download, TrendingUp } from "lucide-react";

export function Reports() {
  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-3xl font-semibold text-gray-900">Reports</h1>
        <p className="text-gray-500 mt-1">
          Generate and export workflow analytics
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <Card className="border-gray-200 shadow-sm hover:shadow-md transition-shadow cursor-pointer">
          <CardHeader>
            <div className="flex items-start justify-between">
              <div>
                <CardTitle className="text-lg">Performance Report</CardTitle>
                <p className="text-sm text-gray-500 mt-1">
                  Weekly workflow metrics
                </p>
              </div>
              <TrendingUp className="w-5 h-5 text-blue-600" />
            </div>
          </CardHeader>
          <CardContent>
            <Button variant="outline" className="w-full">
              <Download className="w-4 h-4 mr-2" />
              Export PDF
            </Button>
          </CardContent>
        </Card>

        <Card className="border-gray-200 shadow-sm hover:shadow-md transition-shadow cursor-pointer">
          <CardHeader>
            <div className="flex items-start justify-between">
              <div>
                <CardTitle className="text-lg">Compliance Report</CardTitle>
                <p className="text-sm text-gray-500 mt-1">SLA adherence data</p>
              </div>
              <FileText className="w-5 h-5 text-green-600" />
            </div>
          </CardHeader>
          <CardContent>
            <Button variant="outline" className="w-full">
              <Download className="w-4 h-4 mr-2" />
              Export PDF
            </Button>
          </CardContent>
        </Card>

        <Card className="border-gray-200 shadow-sm hover:shadow-md transition-shadow cursor-pointer">
          <CardHeader>
            <div className="flex items-start justify-between">
              <div>
                <CardTitle className="text-lg">Case Summary</CardTitle>
                <p className="text-sm text-gray-500 mt-1">
                  Monthly case breakdown
                </p>
              </div>
              <FileText className="w-5 h-5 text-purple-600" />
            </div>
          </CardHeader>
          <CardContent>
            <Button variant="outline" className="w-full">
              <Download className="w-4 h-4 mr-2" />
              Export PDF
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
